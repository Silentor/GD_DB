using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = System.Object;

namespace GDDB.Serialization
{
    //Reader part of GDJson
    public class ObjectsJsonDeserializer : ObjectsJsonCommon
    {
        private          IGdAssetResolver                        _assetResolver;
        private readonly List<UnresolvedGDObjectReference> _unresolvedReferences = new ();
        private readonly List<GDObject>                    _loadedObjects        = new ();     //To resolve loaded references in place
        private readonly CustomSampler _deserObjectSampler     = CustomSampler.Create( "ObjectsJsonSerializer.Deserialize" );
        private readonly CustomSampler _resolveObjectSampler   = CustomSampler.Create( "ObjectsJsonSerializer.ResolveGDObjectReferences" );

        public IReadOnlyList<GDObject> LoadedObjects => _loadedObjects;

        public GDObject Deserialize( String json, IGdAssetResolver assetResolver = null )
        {
            using var strReader = new StringReader( json );
            using var jsonReader = new JsonTextReader( strReader );
            jsonReader.EnsureNextToken( JsonToken.StartObject );
            return Deserialize( jsonReader, assetResolver );
        }

        /// <summary>
        /// Reader stands on StartObject token
        /// </summary>
        /// <param name="json"></param>
        /// <param name="assetResolver"></param>
        /// <returns></returns>
        public GDObject Deserialize( JsonReader json, IGdAssetResolver assetResolver = null )
        {
            _assetResolver    = assetResolver ?? NullGdAssetResolver.Instance;

            _deserObjectSampler.Begin();
            json.EnsureToken( JsonToken.StartObject );
            var result = ReadGDObjectFromJson( json );
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
                                Debug.LogError( $"[{nameof(ObjectsJsonDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
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
                                Debug.LogError( $"[{nameof(ObjectsJsonDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
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
                        Debug.LogError( $"[{nameof(ObjectsJsonDeserializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {unresolvedReference.Guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                    }
                }
            }
            _resolveObjectSampler.End();
        }

        private GDObject ReadGDObjectFromJson( JsonReader json )
        {
            json.EnsureToken( JsonToken.StartObject );
            var name = json.ReadPropertyString( ".Name", false );

            var type = typeof(GDObject);
            var guid = default(Guid);
            var  propName = json.ReadPropertyName();
            if ( propName == ".Type" )
            {
                type = Type.GetType( json.ReadAsString()! );
                guid = Guid.ParseExact( json.ReadPropertyString( ".Ref", false ), "D" );
            }
            else
                guid = Guid.ParseExact( json.ReadPropertyString( ".Ref", true ), "D" );

            var obj     = GDObject.CreateInstance( type ).SetGuid( guid );
            obj.hideFlags     = HideFlags.HideAndDontSave;
            obj.name          = name;

            try
            {
                //Components should be read from reserved property
                json.EnsureNextProperty( ".Components" );
                json.EnsureNextToken( JsonToken.StartArray );

                json.Read();
                var objectStartOrArrayEnd = json.TokenType;
                while( objectStartOrArrayEnd == JsonToken.StartObject )
                {
                    try
                    {
                        var component = (GDComponent)ReadObjectFromJson( json, typeof(GDComponent) );
                        obj.Components.Add( component );
                    }
                    catch ( Exception e )
                    {
                        throw new JsonComponentException( obj.Components.Count, json,
                                $"Error reading component index {obj.Components.Count} of GDObject {obj.Name} ({obj.Guid}) from {json.Path}", e );
                    }

                    json.Read();
                    objectStartOrArrayEnd = json.TokenType;
                }
                json.EnsureToken( JsonToken.EndArray );

                if ( type != typeof(GDObject) )         //There are can be descendants properties
                {
                    json.Read();
                    if( json.TokenType == JsonToken.PropertyName )
                    {
                        ReadObjectPropertiesFromJson( json, obj );
                    }
                }
                else
                    json.EnsureNextToken( JsonToken.EndObject );

                return obj;
            }
            catch ( Exception e )
            {
                throw new JsonObjectException( obj.Name, obj.GetType(), json,
                        $"Error reading object {obj.Name} of type {obj.GetType()} from {json.Path}", e );
            }
        }

        private Object ReadObjectFromJson( JsonReader json, Type propertyType )
        {
            json.EnsureToken( JsonToken.StartObject );
            json.Read();

            if( json.TokenType == JsonToken.EndObject )
                return new Object();                    //Unity doesn't deserialize null objects
            
            json.EnsureToken( JsonToken.PropertyName );

            var objectType = propertyType;
            if ( (String)json.Value == ".Type" )
            {
                var typeStr = json.ReadAsString()!;
                objectType = Type.GetType( typeStr );
                json.Read();                                //Step on object next property name (or end object if no properties present)
                if( objectType == null )
                    throw new InvalidOperationException( $"[{nameof(ObjectsJsonDeserializer)}]-[{nameof(ReadObjectFromJson)}] Cannot create Type from type string '{typeStr}'" );
            }

            var defaultConstructor =
                    objectType.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {objectType} read null object" );
                return null;
            }

            var obj = defaultConstructor.Invoke( Array.Empty<Object>() );
            ReadObjectPropertiesFromJson( json, obj );

            json.EnsureToken( JsonToken.EndObject );

            return obj;
        }

        /// <summary>
        /// Reader stands on PropertyName token or EndObject token
        /// </summary>
        /// <param name="json"></param>
        /// <param name="obj"></param>
        /// <exception cref="JsonPropertyException"></exception>
        private void ReadObjectPropertiesFromJson( JsonReader json, Object obj )
        {
            var propNameOrEndObject = json.TokenType;
            if( propNameOrEndObject == JsonToken.EndObject )
                return;

            var fields = GetTypeFields( obj.GetType() );

            do
            {
                var propName = (String)json.Value;
                var field    = fields.FirstOrDefault( f => f.Name == propName );
                if ( field == null )
                {
                    json.Skip();
                }
                else
                {
                    if ( IsFieldSerializable( field ) )
                    {
                        try
                        {
                            var value = ReadSomethingFromJson( json, field.FieldType );
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
                            throw new JsonPropertyException( field.Name, json, $"Error setting field {field} from json {json.Path}", ex );
                        }
                    }
                    else
                    {
                        json.Skip();
                    }
                }
                json.Read();
                propNameOrEndObject = json.TokenType;
            }
            while( propNameOrEndObject != JsonToken.EndObject );

            json.EnsureToken( JsonToken.EndObject );
        }

        private Object ReadSomethingFromJson( JsonReader json, Type propertyType )
        {
            json.Read();
            if( json.TokenType == JsonToken.EndArray || json.TokenType == JsonToken.EndObject )
                return null;

            //UnityEngine.Debug.Log( $"Reading value {json.Path} of type {propertyType}" );

            if ( propertyType == typeof(Char) )
            {
                return Convert.ChangeType(json.Value, propertyType);
            }
            else if ( propertyType == typeof(String) )
            {
                return (String)json.Value;
            }
            else if ( propertyType.IsPrimitive )
            {
                if( propertyType == typeof(Boolean) )
                    return (Boolean)json.Value;
                if ( json.ValueType == typeof(BigInteger) )
                {
                    var bigInt = (BigInteger)json.Value;
                    if( propertyType == typeof(Int64) )
                        return (Int64)bigInt;
                    if( propertyType == typeof(UInt32) )
                        return (UInt32)bigInt;
                    if( propertyType == typeof(UInt64) )
                        return (UInt64)bigInt;
                    if( propertyType == typeof(Single) )
                        return (Single)bigInt;
                    if( propertyType == typeof(Double) )
                        return (Double)bigInt;
                }
                
                return Convert.ChangeType(json.Value, propertyType);
            }
            else if ( propertyType.IsEnum )
            {
                return Enum.Parse( propertyType, (String)json.Value );
            }
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ) )
            {
                return ReadCollectionFromJson( json, propertyType );
            }
            else if ( typeof(GDObject).IsAssignableFrom( propertyType ) )
            {
                var guidStr = (String)json.Value;
                var guid    = Guid.ParseExact( guidStr, "D" );
                var referencedObj = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                if( referencedObj )
                    return referencedObj;
                else
                    return new UnresolvedGDObjectReference(){Guid = guid};
            }
            else if ( _serializers.TryGetValue( propertyType, out var deserializer ) )
            {
                return deserializer.Deserialize( json );
            }
            else if ( typeof(UnityEngine.Object).IsAssignableFrom( propertyType ) )      //Read Unity asset by reference
            {
                json.EnsureToken( JsonToken.StartObject );
                json.Read();

                if( json.TokenType == JsonToken.EndObject )
                    return null;

                var guidStr = json.ReadPropertyString( ".Ref", true );
                var localId = json.ReadPropertyLong( ".Id", false );

                if( !_assetResolver.TryGetAsset( guidStr, localId, out var asset ) )
                    Debug.LogError( $"Error resolving Unity asset reference {guidStr} : {localId}, type {propertyType}" );
                json.EnsureNextToken( JsonToken.EndObject );
                return asset;
            }
            else
            {
                return ReadObjectFromJson( json, propertyType );
            }
        }
 
        private Object ReadCollectionFromJson( JsonReader json, Type type )
        {
            json.EnsureToken( JsonToken.StartArray );

            if ( type.IsArray )                   //Old Array class not handled
            {
                var elementType = type.GetElementType() ?? typeof(Object);
                if ( elementType == typeof(GDObject) )
                    return GetUnresolvedGDORefCollection( json);

                var bufferType = typeof(List<>).MakeGenericType( elementType );
                var buffer       = (IList)Activator.CreateInstance( bufferType );

                while ( true )
                {
                    var collectionValue = ReadSomethingFromJson( json, elementType );
                    if( json.TokenType == JsonToken.EndArray )
                        break;
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
                    return GetUnresolvedGDORefCollection( json );

                var list        = (IList)Activator.CreateInstance( type );
                while ( true )
                {
                    var collectionValue = ReadSomethingFromJson( json, elementType );
                    if( json.TokenType == JsonToken.EndArray )
                        break;
                    list.Add( collectionValue );
                }

                return list;
            }

            return null;

            //Read collection of references to process later
            UnresolvedGDObjectReference GetUnresolvedGDORefCollection( JsonReader json )
            {
                var references = new List<Guid>();
                json.Read();
                while ( json.TokenType != JsonToken.EndArray)
                {
                    var guidStr = json.ReadAsString()!;
                    references.Add( Guid.ParseExact( guidStr, "D" ) );
                }
               
                return new UnresolvedGDObjectReference()
                                         {
                                                 Guids = references,
                                         };
            }
        }

        private IReadOnlyList<FieldInfo> GetTypeFields( Type type )           //Make it cached
        {
            var result = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            return result;
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
