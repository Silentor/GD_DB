using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Serialization
{
    //Reader part of GDJson
    public partial class ObjectsSerializer
    {
        private List<GDObject>                           _headers;

#region JSON-LINQ reader

        public List<GDObject> Deserialize( JArray json, IGdAssetResolver assetResolver = null )
        {
            _headers          = new List<GDObject>( json.Count );
            _assetResolver    = assetResolver ?? NullGdAssetResolver.Instance;

            //Read GDObject headers
            foreach ( var jObj in json )
            {
                var objHeader = ReadGDObjectHeaderFromJson( (JObject)jObj );
                _headers.Add( objHeader );
            }

            //Read GDObject content (and resolve references)
            for ( var i = 0; i < json.Count; i++ )
            {
                _headers[ i ] = ReadGDObjectContentFromJson( (JObject)json[ i ], _headers[ i ] );
            }

            foreach ( var gdObject in _headers )
            {
                if ( gdObject is ISerializationCallbackReceiver serializationCallbackReceiver )
                    serializationCallbackReceiver.OnAfterDeserialize();

                foreach ( var gdComponent in gdObject.Components )
                    if ( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                        componentSerializationCallbackReceiver.OnAfterDeserialize();

            }

            return _headers;
        }

        private GDObject ReadGDObjectHeaderFromJson( JObject gdObjToken )
        {
            var name    = gdObjToken[ ".Name" ].Value<String>();
            var type    = Type.GetType( gdObjToken[ ".Type" ].Value<String>() );
            var guid    = Guid.ParseExact( gdObjToken[ ".Ref" ].Value<String>(), "D" );
            var enabled = gdObjToken[ ".Enabled" ]?.Value<Boolean>() ?? true;
            var obj     = GDObject.CreateInstance( type ).SetGuid( guid );
            obj.hideFlags     = HideFlags.HideAndDontSave;
            obj.name          = name;
            obj.EnabledObject = enabled;

            return obj;
        }


        private GDObject ReadGDObjectContentFromJson( JObject gdObjToken, GDObject obj )
        {
            try
            {
                //Components should be read from reserved property
                var componentsProp = gdObjToken[ ".Components" ] as JArray;
                for ( var i = 0; i < componentsProp.Count; i++ )
                {
                    var componentjToken = componentsProp[ i ];
                    try
                    {
                        var component = (GDComponent)ReadObjectFromJson( (JObject)componentjToken, typeof(GDComponent) );
                        obj.Components.Add( component );
                    }
                    catch ( Exception e )
                    {
                        throw new JsonComponentException( i, componentjToken,
                                $"Error reading component index {i} of GDObject {obj.Name} ({obj.Guid}) from jtoken {componentjToken} ", e );
                    }
                }

                ReadContentFromJson( gdObjToken, obj );
                return obj;
            }
            catch ( Exception e )
            {
                throw new JsonObjectException( obj.Name, obj.GetType(), gdObjToken,
                        $"Error reading object {obj.Name} of type {obj.GetType()} from jobject {gdObjToken}", e );
            }
        }

        private Object ReadObjectFromJson( JObject jObject, Type propertyType )
        {
            if ( !jObject.HasValues )
                return null;

            var objectType = propertyType;

            //Check polymorphic object
            var valueObj = jObject;
            var typeProp = jObject.Property( ".Type" );
            if ( typeProp != null )
            {
                var typeValue = (String)typeProp.Value;
                objectType = Type.GetType( typeValue );

                if( objectType == null )
                    throw new InvalidOperationException( $"[{nameof(ObjectsSerializer)}]-[{nameof(ReadObjectFromJson)}] Cannot create Type from type string '{typeValue}'" );

                valueObj   = (JObject)jObject[ ".Value" ];
            }

            // if ( _serializers.TryGetValue( objectType, out var serializer ) )
            // {
            //     return serializer.Deserialize( token );
            // }

            var defaultConstructor =
                    objectType.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {objectType} read null object" );
                return null;
            }

            var obj = defaultConstructor.Invoke( Array.Empty<Object>() );

            ReadContentFromJson( valueObj, obj );

            return obj;
        }

        private void ReadContentFromJson( JObject jObject, Object obj )
        {
            foreach ( var field in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                if ( IsFieldSerializable( field ) )
                {
                    var valueToken = jObject[ field.Name ];
                    if ( valueToken == null )                        //Incorrect name
                        field.SetValue( obj, null );
                    else
                        try
                        {
                            var value = ReadSomethingFromJson( valueToken, field.FieldType );
                            field.SetValue( obj, value );
                        }
                        catch ( Exception ex )
                        {
                            throw new JsonPropertyException( field.Name, valueToken, $"Error setting field {field} from jtoken {valueToken}", ex );
                        }
                }
            }
        }

        private Object ReadSomethingFromJson( JToken value, Type propertyType )
        {
            if ( propertyType == typeof(Char) )
            {
                return Convert.ChangeType( value.Value<Object>(), typeof(Char) );
            }
            else if ( propertyType.IsPrimitive || propertyType == typeof(String) )
            {
                return Convert.ChangeType( value.Value<Object>(), propertyType );
            }
            else if ( propertyType.IsEnum )
            {
                return Convert.ChangeType( value.Value<Object>(), propertyType );
            }
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ) )
            {
                return ReadCollectionFromJson( (JArray)value, propertyType );
            }
            else if ( typeof(GDObject).IsAssignableFrom( propertyType ) )
            {
                return ReadGDObjectReferenceFromJson( (JObject)value );
            }
            else if ( _serializers.TryGetValue( propertyType, out var deserializer ) )
            {
                return deserializer.Deserialize( value );
            }
            else if ( typeof(UnityEngine.Object).IsAssignableFrom( propertyType ) )      //Read Unity asset by reference
            {
                if ( !value.HasValues )                  //Seems like missed reference or null reference
                    return null;

                var guid    = value[ ".Ref" ].Value<String>();
                var localId = value[ ".Id" ].Value<long>();
                if( !_assetResolver.TryGetAsset( guid, localId, out var asset ) )
                    Debug.LogError( $"Error resolving Unity asset reference {guid} : {localId}, type {propertyType}" );
                return asset;
            }
            else
            {
                return ReadObjectFromJson( (JObject)value, propertyType );
            }
        }



        private Object ReadCollectionFromJson(JArray token, Type type )
        {
            if ( type.IsArray )
            {
                var values      = token.Children().ToArray();
                var array       = Array.CreateInstance( type.GetElementType(), values.Length );
                var elementType = type.GetElementType();

                for ( int i = 0; i < values.Length; i++ )
                {

                    array.SetValue( ReadSomethingFromJson( values[ i ], elementType ), i );
                }

                return array;
            }
            else if ( type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) )
            {
                var values      = token.Children().ToArray();
                var list        = (IList)Activator.CreateInstance( type );
                var elementType = type.GetGenericArguments()[ 0 ];

                foreach ( var valueToken in values )
                {
                    list.Add( ReadSomethingFromJson( valueToken, elementType ) );
                }

                return list;
            }

            return null;
        }

        private GDObject ReadGDObjectReferenceFromJson( JObject jObject )
        {
            var guid = Guid.Parse( jObject[ ".Ref" ].Value<String>() );
            return _headers.Find( o => o.Guid == guid );
        }

#endregion



#region Fast forward reader

        // public List<GDObject> JsonToGD( String json )
        // {
        //     var result = new List<GDObject>();
        //
        //     using (JsonReader reader = new JsonTextReader( new StringReader( json ) ))
        //     {
        //         EnsureNextToken( reader, JsonToken.StartArray );
        //
        //         while ( reader.Read() )
        //         {
        //             if ( reader.TokenType == JsonToken.StartObject )
        //             {
        //                 var gdObject = ReadGDObjectFromJson( reader );
        //                 result.Add( gdObject );
        //             }
        //             else if ( reader.TokenType == JsonToken.EndArray )
        //             {
        //                 break;
        //             }
        //         }
        //
        //         return result;
        //     }
        // }
        //
        // private GDObject ReadGDObjectFromJson( JsonReader reader )
        // {
        //     EnsureToken( reader, JsonToken.StartObject );
        //     var objectName = ReadPropertyString( reader, "_Name", false );
        //     var objectType = ReadPropertyString( reader, "_Type", false );
        //
        //     var result = (GDObject)ScriptableObject.CreateInstance( objectType );
        //     result.name = objectName;
        //
        //     Debug.Log( $"Start reading object {result.name}" );
        //
        //
        //     while ( reader.Read() )
        //     {
        //         if( reader.TokenType == JsonToken.PropertyName && reader.Depth == 2 )
        //         {
        //             var fieldInfo = result.GetType().GetField( (String)reader.Value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
        //             var value = ReadPropertyFromJson( reader, fieldInfo );
        //
        //             Debug.Log( $"Read {result.name}.{fieldInfo.Name} = {value}" );
        //         }    
        //         else if( reader.TokenType == JsonToken.EndObject && reader.Depth == 1 )
        //             break;
        //     }
        //
        //     //DEBUG skip content
        //     // var objectDepth = reader.Depth - 1;
        //     // while ( reader.Read() )
        //     // {
        //     //     if( reader.Depth == objectDepth && reader.TokenType == JsonToken.EndObject )
        //     //         break;
        //     // }
        //     //DEBUG
        //
        //     EnsureToken( reader, JsonToken.EndObject );
        //     Debug.Log( $"End reading object {result.name}" );
        //
        //     
        //     
        //     return result;
        // }
        //
        // private Object ReadPropertyFromJson( JsonReader reader, FieldInfo fieldInfo )
        // {
        //     if ( reader.Read() )
        //     {
        //         switch ( reader.TokenType )
        //         {
        //             case JsonToken.Null:
        //                 return null;
        //             case JsonToken.Boolean:
        //             case JsonToken.String:
        //             case JsonToken.Integer:
        //             case JsonToken.Float:
        //                 return reader.Value;
        //                 
        //             default:
        //             {
        //
        //                 return null;            //DEBUG read complex objects
        //             }
        //         }
        //     }
        //
        //     return null;
        // }
        //
        // private void EnsureToken( JsonReader reader, JsonToken tokenType )
        // {
        //     if ( reader.TokenType == tokenType )
        //     {
        //         //Its ok
        //     }
        //     else
        //         throw new Exception( $"Expected token {tokenType} but got {reader.TokenType} = {reader.Value}" );
        // }
        //
        // private void EnsureNextToken( JsonReader reader, JsonToken tokenType )
        // {
        //     if ( reader.Read() )
        //     {
        //         EnsureToken( reader, tokenType );
        //     }
        //     else
        //         throw new Exception( $"Unexpected end of file" );
        // }
        //
        // private String ReadPropertyString( JsonReader reader, String propertyName, Boolean alreadyOnStart )
        // {
        //     if( alreadyOnStart )
        //         EnsureToken( reader, JsonToken.PropertyName );
        //     else
        //         EnsureNextToken( reader, JsonToken.PropertyName );
        //     if ( String.Equals(reader.Value, propertyName )) 
        //     {
        //         EnsureNextToken( reader, JsonToken.String );
        //         return (String)reader.Value;
        //
        //     }
        //     else
        //         throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        // }

#endregion

    }
}
