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
    //Reader part of GDJson
    public class GDObjectDeserializer : GDObjectSerializationCommon
    {
        private readonly ReaderBase                                 _reader;

        private          IGdAssetResolver                           _assetResolver;
        private readonly List<UnresolvedGDObjectReference>          _unresolvedReferences = new ();
        private readonly List<GDObject>                             _loadedObjects        = new ();     //To resolve loaded references in place
        private readonly CustomSampler                              _deserObjectSampler   = CustomSampler.Create( $"{nameof(GDObjectDeserializer)}.{nameof(Deserialize)}" );
        private readonly CustomSampler                              _resolveObjectSampler = CustomSampler.Create( $"{nameof(GDObjectDeserializer)}.{nameof(ResolveGDObjectReferences)}" );
        private readonly CustomSampler                              _getTypeConstructorSampler = CustomSampler.Create( $"{nameof(GDObjectDeserializer)}.{nameof(GetTypeConstructor)}" );
        private readonly Dictionary<Type, IReadOnlyList<FieldInfo>> _fieldsCache          = new();
        private readonly Dictionary<Type, ConstructorInfo>          _constructorCache     = new();

        public IReadOnlyList<GDObject> LoadedObjects => _loadedObjects;

        public GDObjectDeserializer( ReaderBase reader )
        {
            _reader = reader;

            reader.SetAlias( 101, EToken.PropertyName, NameTag );            //Common to Folders
            reader.SetAlias( 102, EToken.PropertyName, IdTag );                //Common to Folders
            //reader.SetPropertyNameAlias( 2, ".folders" );
            //reader.SetPropertyNameAlias( 3, ".objs" );
            reader.SetAlias( 105, EToken.PropertyName, TypeTag );
            reader.SetAlias( 106, EToken.PropertyName, EnabledTag );
            reader.SetAlias( 107, EToken.PropertyName, ComponentsTag );
            reader.SetAlias( 108, EToken.PropertyName, LocalIdTag );

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
        public GDObject Deserialize( IGdAssetResolver? assetResolver = null )
        {
            _assetResolver    = assetResolver ?? NullGdAssetResolver.Instance;

            _deserObjectSampler.Begin();

            _reader.SeekStartObject(  );
            var result = ReadGDObject( _reader );

            _deserObjectSampler.End();

            if( result is ISerializationCallbackReceiver serializationCallbackReceiver)
                serializationCallbackReceiver.OnAfterDeserialize();

            foreach ( var gdComponent in result.Components )
                if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                    componentSerializationCallbackReceiver.OnAfterDeserialize();

            _loadedObjects.Add( result );
            return result;
        }

        public void ResolveGDObjectReferences( )
        {
            _resolveObjectSampler.Begin();
            foreach ( var unresolvedReference in _unresolvedReferences )
            {
                if( unresolvedReference.Guids != null )                //Collection target field
                {
                    if ( unresolvedReference.Field.FieldType.IsArray )
                    {
                        var result = Array.CreateInstance( unresolvedReference.Field.FieldType.GetElementType(), unresolvedReference.Guids.Count );
                        for ( int i = 0; i < unresolvedReference.Guids.Count; i++ )
                        {
                            var guid           = unresolvedReference.Guids[i];
                            var resolvedObject = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                            if ( resolvedObject )
                            {
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
                        var result = (IList)Activator.CreateInstance( unresolvedReference.Field.FieldType );
                        for ( int i = 0; i < unresolvedReference.Guids.Count; i++ )
                        {
                            var guid           = unresolvedReference.Guids[i];
                            var resolvedObject = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                            if ( resolvedObject )
                            {
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
                    var resolvedObject = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == unresolvedReference.Guid );
                    if( resolvedObject )
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, resolvedObject );
                    else
                    {
                        Debug.LogError( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {unresolvedReference.Guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                    }
                }
            }
            _resolveObjectSampler.End();
        }

        private GDObject ReadGDObject( ReaderBase reader )
        {
            reader.EnsureStartObject();
            var name = reader.ReadPropertyString( NameTag );

            var    type     = typeof(GDObject);
            var    guid     = Guid.Empty;
            var    propName = reader.ReadPropertyName();
            String typeName = null;
            if ( propName == TypeTag )
            {
                type = reader.ReadTypeValue( typeof(GDObject).Assembly );
                guid     = reader.ReadPropertyGuid( IdTag );
            }
            else if( propName == IdTag )
            {
                guid = reader.ReadGuidValue();
            }
            else throw new Exception($"[{nameof(GDObjectDeserializer)}]-[{nameof(ReadGDObject)}] unknown property {propName}");

            if ( type == null )
                throw new ReaderObjectException( name, null, reader, $"Cannot find the type {typeName}, object name {name}" );

            var obj     = GDObject.CreateInstance( type ).SetGuid( guid );
            obj.hideFlags     = HideFlags.HideAndDontSave;
            obj.name          = name;

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
                return obj;
            }
            catch ( Exception e )
            {
                throw new ReaderObjectException( obj.Name, obj.GetType(), reader,
                        $"Error reading object {obj.Name} of type {obj.GetType()} from {reader.Path}", e );
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
                objectType = reader.ReadTypeValue( propertyType.Assembly );
                if( objectType == null )
                    throw new InvalidOperationException( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ReadObject)}] Cannot read the type. Property type for this object (destination type) is {propertyType}" );
                reader.ReadNextToken();         //Stand on next property name or EndObject
            }

            var defaultConstructor = GetTypeConstructor( objectType );
            if ( defaultConstructor == null )
            {
                throw new InvalidOperationException( $"[{nameof(GDObjectDeserializer)}]-[{nameof(ReadObject)}] Default constructor of type {objectType} is not found" );
            }

            var obj = defaultConstructor.Invoke( Array.Empty<Object>() );

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

            var fields = GetTypeFields( obj.GetType() );

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
                    if ( IsFieldSerializable( field ) )
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
                    else
                    {
                        reader.SkipProperty();
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
            else if ( typeof(GDObject).IsAssignableFrom( propertyType ) )                 //Read GDObject by reference
            {
                var guid = reader.GetGuidValue();
                var referencedObj = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                if( referencedObj )
                    return referencedObj;
                else
                    return new UnresolvedGDObjectReference(){Guid = guid};
            }
            else if ( _serializers.TryGetValue( propertyType, out var deserializer ) )
            {
                var result = deserializer.DeserializeBase( reader );
                //reader.ReadNextToken();
                return result;
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

        private IReadOnlyList<FieldInfo> GetTypeFields( Type type )           
        {
            if ( !_fieldsCache.TryGetValue( type, out var fields ) )
            {
                fields = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
                _fieldsCache.Add( type, fields );
            }

            return fields;
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

    }
}
