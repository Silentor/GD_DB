using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB
{
    public class GDJson
    {
        public Dictionary<Type, GdJsonCustomSerializer> _serializers = new();

        public GDJson( )
        {
            _serializers.Add( typeof(Vector3), new Vector3Serializer() );
        }

        public String GDToJson( GdLoader gdLoader )
        {
            JArray resulObjects = new JArray();

            foreach ( var gdObj in gdLoader.AllObjects )
            {
                resulObjects.Add(  WriteObjectToJson( gdObj ) );                    
            }

            var result = resulObjects.ToString();
            Debug.Log( $"Written {gdLoader.AllObjects.Count} gd objects to json string {result.Length} symbols" );

            return result;
        }

        private void WriteNullPropertyToJson( String name, JsonWriter writer )
        {
            writer.WritePropertyName(name);
            writer.WriteNull();
        }

        

        private JObject WriteObjectToJson( GDObject obj )
        {
            var result = new JObject();
            result.Add( ".Name", obj.name );
            var type = obj.GetType();
            result.Add( ".Type", type.Assembly == GetType().Assembly ? type.FullName : type.AssemblyQualifiedName );

            WriteObjectContent( type, obj, result );

            return result;
        }

        private JObject WriteObjectToJson( Type propertyType, Object obj )
        {
            var result = new JObject();
            var value  = result;

            var actualType = obj.GetType();

            //Check for polymorphic object
            if ( propertyType != actualType )
            {
                result.Add( ".Type", actualType.Assembly == GetType().Assembly ? actualType.FullName : actualType.AssemblyQualifiedName );
                value = new JObject();
                result.Add( ".Value", value );
            }

            // if( _serializers.TryGetValue( obj.GetType(), out var serializer ) )
            // {
            //     serializer.Serialize( obj, writer ) ;
            //     return;
            // }
            // else
            {
                WriteObjectContent( actualType, obj, value );
            }

            return result;
        }

        private void WriteObjectContent( Type actualType, Object obj, JObject writer )
        {
            foreach (var field in actualType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                //Skip readonly and const fields
                if( field.IsInitOnly || field.IsLiteral )
                    continue;

                if( IsFieldSerializable( field ))
                {
                    var value = field.GetValue(obj);
                    var property = WritePropertyToJson( field.Name, field.FieldType, value, value.GetType() );
                    writer.Add( property );
                }
            }
        }

       
        private JProperty WritePropertyToJson( String propertyName, Type propertyType, Object value, Type valueType )
        {
            var result = new JProperty( propertyName );
            result.Value = WriteSomethingToJson( propertyType, value, valueType );
            return result;
        }

        private JToken WriteSomethingToJson(Type propertyType, Object value, Type valueType )
        {
            if( valueType.IsPrimitive || valueType == typeof(String) )
            {
                return new JValue( value );
            }
            else if( valueType.IsArray)
            {
                var elementType = valueType.GetElementType();
                return WriteCollectionToJson( elementType, (IEnumerable)value );
            }
            else if( valueType.IsGenericType && valueType.GetGenericTypeDefinition() ==  typeof(List<>))
            {
                var elementType = valueType.GetGenericArguments()[0];
                return WriteCollectionToJson( elementType, (IEnumerable)value );
            }
            else
            {
                return WriteObjectToJson( propertyType, value );
            }
        }

        private JArray WriteCollectionToJson( Type elementType, IEnumerable collection )
        {
            var result = new JArray();
            foreach (var obj in collection)
            {
                result.Add( WriteSomethingToJson( elementType, obj, obj.GetType() ) );
            }

            return result;
        }


    #region JSON-LINQ reader

       public List<GDObject> JsonToGD( String json )
        {
            using (var reader = new StringReader( json ) )
            {
                var o       = (JArray)JToken.ReadFrom(new JsonTextReader(reader));
                var content = o.Children<JObject>();
                var result  = new List<GDObject>();
        
                foreach ( var gdObject in content )
                {
                    var resultObj = ReadGDObjectFromJson( gdObject );
                    result.Add( resultObj );
                }
        
                return result;
            }
        }

        

        private GDObject ReadGDObjectFromJson( JObject gdObjToken )
        {
            var name = gdObjToken[".Name"].Value<String>();
            var type = Type.GetType( gdObjToken[".Type"].Value<String>() );
            var obj  = (GDObject)ScriptableObject.CreateInstance( type );
            obj.name = name;
        
            ReadContentFromJson( gdObjToken, obj );
        
            return obj;
        }

        private Object ReadObjectFromJson( JObject jObject, Type propertyType )
        {
            var objectType = propertyType;

            //Check polymorphic object
            var valueObj = jObject;
            var typeProp = jObject.Property(".Type");
            if ( typeProp != null )
            {
                objectType = Type.GetType( jObject[".Type"].Value<String>() );
                valueObj = (JObject)jObject[".Value"];
            }

            // if ( _serializers.TryGetValue( objectType, out var serializer ) )
            // {
            //     return serializer.Deserialize( token );
            // }

            var defaultConstructor = objectType.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {objectType} read null object" );
                return null;
            }
            var obj = Activator.CreateInstance( objectType );
        
            ReadContentFromJson( valueObj, obj );
        
            return obj;
        }

        private void ReadContentFromJson( JObject jObject, Object obj )
        {
            foreach (var field in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( IsFieldSerializable( field ) )
                {
                    var valueToken = jObject[field.Name];
                    if( valueToken == null )
                        field.SetValue( obj, null );
                    else
                        try
                        {
                            var value = ReadSomethingFromJson( valueToken, field.FieldType );
                            field.SetValue( obj, value );
                        }
                        catch ( Exception ex )  
                        {
                            Debug.LogError( $"Error settings field {field} from jtoken {valueToken.Type} value {valueToken}, exception: {ex}" );                            
                        }
                }
            }
        }
        
        private Object ReadSomethingFromJson( JToken value, Type propertyType )
        {
            if( propertyType.IsPrimitive || propertyType == typeof(String) )
            {
                return Convert.ChangeType( value.Value<Object>(), propertyType );
            }
            else if( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ))
            {
                return ReadCollectionFromJson( (JArray)value, propertyType );
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
                    
                    array.SetValue( ReadSomethingFromJson( values[i], elementType ), i );
                }
        
                return array;
            }
            else if ( type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) )
            {
                var values = token.Children().ToArray();
                var list = (IList)Activator.CreateInstance( type );    
                var elementType = type.GetGenericArguments()[0];
        
                foreach ( var valueToken in values )
                {
                    list.Add( ReadSomethingFromJson( valueToken, elementType ) );
                }
        
                return list;
            }
        
            return null;
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

        private static Boolean IsFieldSerializable(FieldInfo field )
        {
            return (field.IsPublic && !field.IsDefined( typeof(NonSerializedAttribute), false )) || field.IsDefined( typeof(SerializeField), false );
        }

    }
}