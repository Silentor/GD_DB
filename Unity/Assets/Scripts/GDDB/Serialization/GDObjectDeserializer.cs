using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = System.Object;

namespace GDDB.Serialization
{
    /// <summary>
    /// Reads gd objects from reader, stores cross gd objects references and able to resolve them after all objects are read
    /// </summary>
    public class GDObjectDeserializer : GDObjectSerializationCommon
    {
        private readonly ReaderBase                                 _reader;

        private          IGdAssetResolver                           _assetResolver;
        private readonly List<UnresolvedGDObjectReference>          _unresolvedReferences = new ();
        private readonly List<GDObjectAndGuid>                      _loadedObjects        = new ();     //To resolve gd objects cross-references
        private readonly CustomSampler                              _deserObjectSampler   = CustomSampler.Create( $"{nameof(GDObjectDeserializer)}.{nameof(Deserialize)}" );
        private readonly CustomSampler                              _resolveObjectSampler = CustomSampler.Create( $"{nameof(GDObjectDeserializer)}.{nameof(ResolveGDObjectReferences)}" );
        private readonly CustomSampler                              _getTypeConstructorSampler = CustomSampler.Create( $"{nameof(GDObjectDeserializer)}.{nameof(GetTypeConstructor)}" );
        private readonly Dictionary<Type, ConstructorInfo>          _constructorCache     = new();

        public IReadOnlyList<GDObjectAndGuid> LoadedObjects => _loadedObjects;

        public GDObjectDeserializer( ReaderBase reader )
        {
            _reader = reader;

#if UNITY_2021_2_OR_NEWER
            AddSerializer( new Vector3Serializer() );
            AddSerializer( new Vector3IntSerializer() );
            AddSerializer( new Vector2Serializer() );
            AddSerializer( new Vector2IntSerializer() );
            AddSerializer( new Vector4Serializer() );
            AddSerializer( new QuaternionSerializer() );
            AddSerializer( new Matrix4x4Serializer() );
            AddSerializer( new RectSerializer() );
            AddSerializer( new BoundsSerializer() );
            AddSerializer( new Color32Serializer() );
            AddSerializer( new ColorSerializer() );
            AddSerializer( new AnimationCurveSerializer() );
#endif
        }

        // public GDObject Deserialize( String json, IGdAssetResolver? assetResolver = null )
        // {
        //     using var strReader = new StringReader( json );
        //     using var jsonReader = new JsonTextReader( strReader );
        //     jsonReader.EnsureNextToken( JsonToken.StartObject );
        //     return Deserialize( jsonReader, assetResolver );
        // }

        /// <summary>
        /// Reader stands on StartObject token
        /// </summary>
        /// <param name="assetResolver"></param>
        /// <returns></returns>
        public ScriptableObject Deserialize( IGdAssetResolver? assetResolver = null )
        {
            _assetResolver    = assetResolver ?? NullGdAssetResolver.Instance;

            _deserObjectSampler.Begin();

            _reader.SeekStartObject(  );
            var result = ReadGDObject( _reader );

            _deserObjectSampler.End();

            if( result is ISerializationCallbackReceiver serializationCallbackReceiver)
                serializationCallbackReceiver.OnAfterDeserialize();

            if( result is GDObject gdObject )
                foreach ( var gdComponent in gdObject.Components )
                    if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                        componentSerializationCallbackReceiver.OnAfterDeserialize();
            
            return result;
        }

        public void ResolveGDObjectReferences( )
        {
            _resolveObjectSampler.Begin();
            _loadedObjects.Sort();
            foreach ( var unresolvedReference in _unresolvedReferences )
            {
                if( unresolvedReference.Guids != null )                //Collection target field
                {
                    if ( unresolvedReference.Field.FieldType.IsArray )             //Array of gdobject references
                    {
                        var result = Array.CreateInstance( unresolvedReference.Field.FieldType.GetElementType(), unresolvedReference.Guids.Count );
                        for ( int i = 0; i < unresolvedReference.Guids.Count; i++ )
                        {
                            var guid           = unresolvedReference.Guids[i];
                            var resolvedObjectIndex = _loadedObjects.BinarySearch( new GDObjectAndGuid(){Guid = guid} );
                            if ( resolvedObjectIndex >= 0 )
                            {
                                var resolvedObject = _loadedObjects[resolvedObjectIndex].GdObject;
                                result.SetValue( resolvedObject, i );
                            }
                            else
                            {
                                Debug.LogError( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                            }
                        }
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, result );
                    }
                    else
                    {
                        var result = (IList)Activator.CreateInstance( unresolvedReference.Field.FieldType );    //List of gdobject references
                        for ( int i = 0; i < unresolvedReference.Guids.Count; i++ )
                        {
                            var guid                = unresolvedReference.Guids[i];
                            var resolvedObjectIndex = _loadedObjects.BinarySearch( new GDObjectAndGuid(){Guid = guid} );
                            if ( resolvedObjectIndex >= 0 )
                            {
                                var resolvedObject      = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                                result.Add( resolvedObject );
                            }
                            else
                            {
                                Debug.LogError( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                            }
                        }
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, result );
                    }
                }
                else                                 //Scalar target field
                {
                    var resolvedObjectIndex = _loadedObjects.BinarySearch( new GDObjectAndGuid(){Guid = unresolvedReference.Guid} );
                    if ( resolvedObjectIndex >= 0 )
                    {
                        var resolvedObject      = _loadedObjects[resolvedObjectIndex].GdObject;
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, resolvedObject );
                    }
                    else
                    {
                        Debug.LogError( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {unresolvedReference.Guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                    }
                }
            }
            _resolveObjectSampler.End();
        }

        private ScriptableObject ReadGDObject( ReaderBase reader )
        {
            reader.EnsureStartObject();
            var name = reader.ReadPropertyString( NameTag );

            var    type     = typeof(GDObject);
            var    guid     = Guid.Empty;
            var    propName = reader.ReadPropertyName();
            String typeName = null;
            if ( propName == TypeTag )
            {
                type = reader.ReadTypeValue(  );
                guid = reader.ReadPropertyGuid( IdTag );
            }
            else if( propName == IdTag )
            {
                guid = reader.ReadGuidValue();
            }
            else throw new Exception($"[{nameof(GDObjectDeserializer)}]-[{nameof(ReadGDObject)}] unknown property {propName}");

            if ( type == null )
                throw new ReaderObjectException( name, null, reader, $"Cannot find the type {typeName}, object name {name}" );

            if ( typeof(GDObject).IsAssignableFrom( type ) )
            {    
                var obj       = GDObject.CreateInstance( type ).SetGuid( guid );
                obj.hideFlags = HideFlags.HideAndDontSave;
                obj.name      = name;

                try
                {
                    //Components should be read from reserved property
                    reader.ReadNextToken();         reader.EnsurePropertyName( ComponentsTag );
                    reader.ReadStartArray();

                    while ( reader.ReadNextToken() != EToken.EndArray )
                    {
                        reader.EnsureStartObject();

                        try
                        {
                            var component = (GDComponent)ReadObject( reader, typeof(GDComponent) );
                            obj.Components.Add( component );
                        }
                        catch ( Exception e )
                        {
                            throw new ReaderComponentException( obj.Components.Count, reader,
                                    $"Error reading component index {obj.Components.Count} of GDObject {obj.Name} ({obj.Guid}) from {reader.Path}", e );
                        }
                    }
                    reader.EnsureEndArray(  );

                    reader.ReadNextToken();
                    if ( type != typeof(GDObject) && reader.CurrentToken != EToken.EndObject )         //There can be properties of descendants of GDObject type
                    {
                        ReadObjectProperties( reader, obj );
                    }

                    reader.EnsureEndObject();          
                    _loadedObjects.Add( new GDObjectAndGuid(){Guid = guid, GdObject = obj} );
                    return obj;
                }
                catch ( Exception e )
                {
                    throw new ReaderObjectException( obj.Name, obj.GetType(), reader,
                            $"Error reading object {obj.Name} of type {obj.GetType()} id {obj.Guid} from {reader.Path}", e );
                }
            }
            else                                                           //Read plain ScriptableObject
            {
                var obj           = ScriptableObject.CreateInstance( type );
                obj.hideFlags = HideFlags.HideAndDontSave;
                obj.name      = name;

                try
                {
                    reader.ReadNextToken();
                    if ( type != typeof(ScriptableObject) && reader.CurrentToken != EToken.EndObject )         //There can be properties of descendants of Scriptable object type
                    {
                        ReadObjectProperties( reader, obj );
                    }

                    reader.EnsureEndObject();
                    _loadedObjects.Add( new GDObjectAndGuid(){Guid = guid, GdObject = obj} );
                    return obj;
                }
                catch ( Exception e )
                {
                    throw new ReaderObjectException( obj.name, obj.GetType(), reader,
                            $"Error reading object {obj.name} of type {obj.GetType()} id {guid} from {reader.Path}", e );
                }

            }
        }

        private Object ReadObject( ReaderBase reader, Type propertyType )
        {
            reader.EnsureStartObject();

            if ( reader.ReadNextToken() == EToken.EndObject )
                return null;
            
            reader.EnsureToken( EToken.PropertyName );

            var objectType = propertyType;
            if ( reader.GetPropertyName() == TypeTag )
            {
                objectType = reader.ReadTypeValue(  );
                if( objectType == null )
                    throw new InvalidOperationException( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ReadObject)}] Cannot read the type. Property type for this object (destination type) is {propertyType}" );
                reader.ReadNextToken();         //Stand on next property name or EndObject
            }

            Object obj;
            if ( objectType.IsClass )
            {
                var defaultConstructor = GetTypeConstructor( objectType );
                if ( defaultConstructor == null )
                {
                    throw new InvalidOperationException( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ReadObject)}] Default constructor of type {objectType} is not found" );
                }
                obj = defaultConstructor.Invoke( Array.Empty<Object>() );
            }
            else
            {
                obj = Activator.CreateInstance( objectType );     //Structs
            }

            if( reader.CurrentToken != EToken.EndObject )
                ReadObjectProperties( reader, obj );

            reader.EnsureEndObject();

            return obj;
        }


        /// <summary>
        /// Reader stands on PropertyName token or EndObject token
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="obj"></param>
        /// <exception cref="ReaderPropertyException"></exception>
        private void ReadObjectProperties( ReaderBase reader, Object obj )
        {
            var propNameOrEndObject = reader.CurrentToken;
            if( propNameOrEndObject == EToken.EndObject )
                return;

            var fields = GetSerializableFields( obj.GetType() );

            do
            {
                var propName = reader.GetPropertyName();
                var field    = fields.FirstOrDefault( f => f.Name == propName );
                if ( field == null )
                {
                    reader.SkipProperty();
                }
                else
                {
                    try
                    {
                        reader.ReadNextToken();
                        var value = ReadSomething( reader, field.FieldType );
                        if ( value is UnresolvedGDObjectReference gdRef )                        //Should be resolved after all objects deserialization
                        {
                            gdRef.TargetObject = obj;
                            gdRef.Field        = field;
                            _unresolvedReferences.Add( gdRef );
                        }
                        else
                            field.SetValue( obj, value );
                    }
                    catch ( Exception ex )
                    {
                        throw new ReaderPropertyException( field.Name, reader, $"Error setting field {field} from json {reader.Path}", ex );
                    }
                }
                reader.ReadNextToken();
                propNameOrEndObject = reader.CurrentToken;
            }
            while( propNameOrEndObject != EToken.EndObject );

            reader.EnsureToken( EToken.EndObject );
        }

        private Object ReadSomething( ReaderBase reader, Type propertyType )
        {
            //if( reader.CurrentToken == EToken.EndArray || reader.CurrentToken == EToken.EndObject )
                //return null;

            //UnityEngine.Debug.Log( $"Reading value {json.Path} of type {propertyType}" );

            if ( propertyType == typeof(Char) )
            {
                return Convert.ChangeType(reader.GetStringValue(), propertyType);
            }
            else if ( propertyType == typeof(String) )
            {
                return reader.GetStringValue();
            }
            else if ( propertyType.IsPrimitive )
            {
                if( propertyType == typeof(Boolean) )
                    return reader.GetBoolValue();
                if ( propertyType == typeof(UInt64) )
                    return reader.GetUInt64Value();
                if( propertyType == typeof(Double) )
                    return reader.GetDoubleValue();
                if( propertyType == typeof(Single) )
                    return reader.GetSingleValue();
                
                return Convert.ChangeType(reader.GetIntegerValue(), propertyType);
            }
            else if ( propertyType.IsEnum )
            {
                return reader.GetEnumValue( propertyType );
            }
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ) )
            {
                return ReadCollectionFromJson( reader, propertyType );
            }
            else if ( _serializers.TryGetValue( propertyType, out var deserializer ) )
            {
                var result = deserializer.DeserializeBase( reader );
                return result;
            }
            else if ( typeof(ScriptableObject).IsAssignableFrom( propertyType ) )                 //Read GDObject by reference
            {
                var guid                = reader.GetGuidValue();
                if ( guid == Guid.Empty )
                    return null;
                var resolvedObjectIndex = _loadedObjects.BinarySearch( new GDObjectAndGuid(){Guid = guid} );
                if( resolvedObjectIndex >= 0 )
                {
                    return _loadedObjects[resolvedObjectIndex].GdObject;
                }
                else
                    return new UnresolvedGDObjectReference(){Guid = guid};
            }
            else if ( typeof(UnityEngine.Object).IsAssignableFrom( propertyType ) )      //Read Unity asset by reference
            {
                reader.EnsureStartObject();
                reader.ReadNextToken();

                if( reader.CurrentToken == EToken.EndObject )
                    return null;

                reader.EnsurePropertyName( IdTag );            
                var id = reader.ReadStringValue( );
                var localId = reader.ReadPropertyInteger( LocalIdTag );

                if( !_assetResolver.TryGetAsset( id, localId, out var asset ) )
                    Debug.LogError( $"Error resolving Unity asset reference {id} : {localId}, type {propertyType}" );
                reader.ReadEndObject();
                return asset;
            }
            else
            {
                return ReadObject( reader, propertyType );
            }
        }
 
        private Object ReadCollectionFromJson( ReaderBase reader, Type type )
        {
            reader.EnsureStartArray( );

            if ( type.IsArray )                   //Old Array class not handled
            {
                var elementType = type.GetElementType() ?? typeof(Object);
                if ( elementType == typeof(GDObject) )
                    return GetUnresolvedGDORefCollection( reader);

                var bufferType = typeof(List<>).MakeGenericType( elementType );
                var buffer       = (IList)Activator.CreateInstance( bufferType );

                while( reader.ReadNextToken() != EToken.EndArray )
                {
                    var collectionValue = ReadSomething( reader, elementType );
                    buffer.Add( collectionValue );
                }
                
                var result = Array.CreateInstance( elementType, buffer.Count );
                for ( int i = 0; i < result.Length; i++ )
                {
                    result.SetValue( buffer[i], i );
                }
                return result;
            }
            else if ( type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) )
            {
                var elementType = type.GetGenericArguments()[ 0 ];
                if ( elementType == typeof(GDObject) )
                    return GetUnresolvedGDORefCollection( reader );

                var list        = (IList)Activator.CreateInstance( type );
                while ( reader.ReadNextToken() != EToken.EndArray )
                {
                    var collectionValue = ReadSomething( reader, elementType );
                    list.Add( collectionValue );
                }

                return list;
            }

            return null;

            //Read collection of references to process later
            UnresolvedGDObjectReference GetUnresolvedGDORefCollection( ReaderBase reader )
            {
                var references = new List<Guid>();
                while ( reader.ReadNextToken() != EToken.EndArray)
                {
                    var guidStr = reader.GetStringValue();
                    references.Add( Guid.ParseExact( guidStr, "D" ) );
                }
               
                return new UnresolvedGDObjectReference()
                                         {
                                                 Guids = references,
                                         };
            }
        }

        private ConstructorInfo GetTypeConstructor(Type type )
        {
            _getTypeConstructorSampler.Begin();
            if ( !_constructorCache.TryGetValue( type, out var con ) )
            {
                con = type.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Array.Empty<Type>(), null );
                _constructorCache.Add( type, con );
            }
            _getTypeConstructorSampler.End();

            return con;
        }

        private struct UnresolvedGDObjectReference
        {
            public Guid       Guid;             //GDObject field
            public List<Guid> Guids;            //or collection of GDObjects field

            //Target info
            public Object  TargetObject;
            public FieldInfo Field;
        }

        public struct GDObjectAndGuid : IEquatable<GDObjectAndGuid>, IComparable<GDObjectAndGuid>
        {
            public ScriptableObject GdObject;
            public Guid     Guid;

            public bool Equals(GDObjectAndGuid other)
            {
                return Guid.Equals( other.Guid );
            }

            public override bool Equals(object obj)
            {
                return obj is GDObjectAndGuid other && Equals( other );
            }

            public override int GetHashCode( )
            {
                return Guid.GetHashCode();
            }

            public static bool operator ==(GDObjectAndGuid left, GDObjectAndGuid right)
            {
                return left.Equals( right );
            }

            public static bool operator !=(GDObjectAndGuid left, GDObjectAndGuid right)
            {
                return !left.Equals( right );
            }

            public int CompareTo(GDObjectAndGuid other)
            {
                return Guid.CompareTo( other.Guid );
            }
        }

    }
}
