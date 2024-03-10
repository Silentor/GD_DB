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
            StringWriter sw = new StringWriter( CultureInfo.InvariantCulture );
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartArray();

                foreach ( var gdObj in gdLoader.AllObjects )
                {
                    WriteObjectToJson( gdObj, writer );                    
                }

                writer.WriteEndArray();
            }

            var result = sw.ToString();

            Debug.Log( $"Written {gdLoader.AllObjects.Count} gd objects to json string {result.Length} symbols" );

            return sw.ToString();
        }

        private void WriteNullPropertyToJson( String name, JsonWriter writer )
        {
            writer.WritePropertyName(name);
            writer.WriteNull();
        }

        

        private void WriteObjectToJson( GDObject obj, JsonWriter writer )
        {
            writer.WriteStartObject();

            writer.WritePropertyName( ".Name" );
            writer.WriteValue( obj.name );
            writer.WritePropertyName( ".Type" );
            var type = obj.GetType();
            writer.WriteValue( type.Assembly == GetType().Assembly ? type.FullName : type.AssemblyQualifiedName );

            WriteObjectContent( type, obj, writer );

            writer.WriteEndObject();
        }

        private void WriteObjectToJson( Type propertyType, Object obj, JsonWriter writer )
        {
            var actualType = obj.GetType();

            //Check for polymorphic object
            if ( propertyType != actualType )
            {
                writer.WriteStartObject();
                writer.WritePropertyName( ".Type" );
                writer.WriteValue( actualType.Assembly == GetType().Assembly ? actualType.FullName : actualType.AssemblyQualifiedName );
                writer.WritePropertyName( ".Value" );
            }

            if( _serializers.TryGetValue( obj.GetType(), out var serializer ) )
            {
                serializer.Serialize( obj, writer ) ;
                return;
            }
            else
            {
                writer.WriteStartObject();
                WriteObjectContent( actualType, obj, writer );
                writer.WriteEndObject();
            }
                        
            //Check for polymorphic object
            if ( propertyType != actualType )
            {
                writer.WriteEndObject();
            }
            
        }

        private void WriteObjectContent( Type actualType, Object obj, JsonWriter writer )
        {
            foreach (var field in actualType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( field.IsPublic || field.IsDefined( typeof(SerializeField), false ))
                {
                    var value = field.GetValue(obj);
                    WritePropertyToJson( field.Name, field.FieldType, value, value.GetType(), writer );
                }
            }
        }

        private void WritePropertyToJson( String propertyName, Type propertyType, Object value, Type valueType, JsonWriter writer )
        {
            writer.WritePropertyName(propertyName);

            WriteSomethingToJson( propertyType, value, valueType, writer );
        }

        private void WriteSomethingToJson(Type propertyType, Object value, Type valueType, JsonWriter writer )
        {
            if( valueType.IsPrimitive || valueType == typeof(String) )
            {
                writer.WriteValue(value);
            }
            else if( valueType.IsArray)
            {
                var elementType = valueType.GetElementType();
                WriteCollectionToJson( elementType, (IEnumerable)value, writer );
            }
            else if( valueType.IsGenericType && valueType.GetGenericTypeDefinition() ==  typeof(List<>))
            {
                var elementType = valueType.GetGenericArguments()[0];
                WriteCollectionToJson( elementType, (IEnumerable)value, writer );
            }
            else
            {
                WriteObjectToJson( propertyType, value, writer );
            }
        }

        private void WriteCollectionToJson( Type elementType, IEnumerable collection, JsonWriter writer )
        {
            writer.WriteStartArray();
            foreach (var obj in collection)
            {
                WriteSomethingToJson( elementType, obj, obj.GetType(), writer );
            }
            writer.WriteEndArray();
        }


    #region JSON-LINQ reader

       public List<GDObject> JsonToGD( String json )
        {
            using (var reader = new StringReader( json ) )
            {
                var o       = JToken.ReadFrom(new JsonTextReader(reader));
                var content = o.Children();
                var result  = new List<GDObject>();
        
                foreach ( var gdObject in content )
                {
                    var resultObj = ReadGDObjectFromJson( (JObject)gdObject );
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
        
            foreach (var field in type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( field.IsPublic || field.IsDefined( typeof(SerializeField), false ))
                {
                    var prop = gdObjToken.Property(field.Name);
                    if( prop == null )
                        field.SetValue( obj, null );
                    else
                    {
                        try
                        {
                            field.SetValue( obj, ReadPropertyFromJson( prop, field.FieldType ) );
                        }
                        catch ( Exception ex )  
                        {
                            Debug.LogError( $"Error settings field {field} from jtoken {prop.Type} value {prop}, exception: {ex}" );                            
                        }
                    }
                }
            }
        
            return obj;
        }
        
        private Object ReadPropertyFromJson( JProperty token, Type propertyType )
        {
            if( propertyType.IsPrimitive || propertyType == typeof(String) )
            {
                return Convert.ChangeType( token.Value<Object>(), propertyType );
            }
            else if( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ))
            {
                return ReadCollectionFromJson( (JArray)token.Value, propertyType );
            }
            else
            {
                return ReadObjectFromJson( (JObject)token.Value, propertyType );
            }
        }
        
        private Object ReadObjectFromJson( JObject token, Type propertyType )
        {
            var objectType = propertyType;

            var typeProp   = token.Property(".Type");
            if ( typeProp != null )
            {
                objectType = Type.GetType( token[".Type"].Value<String>() );
            }

            if ( _serializers.TryGetValue( objectType, out var serializer ) )
            {
                return serializer.Deserialize( token );
            }

            var defaultConstructor = objectType.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {objectType}" );
                return null;
            }
            var obj  = Activator.CreateInstance( objectType );
        
            foreach (var field in objectType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( field.IsPublic || field.IsDefined( typeof(SerializeField), false ))
                {
                    var value = token[field.Name];
                    if( value == null )
                        field.SetValue( obj, null );
                    else
                        field.SetValue( obj, ReadPropertyFromJson( value, field.FieldType ) );
                }
            }
        
            return obj;
        }
        
        private Object ReadCollectionFromJson(JArray token, Type type )
        {
            if ( type.IsArray )
            {
                var values = token.Children().ToArray();
                var array  = Array.CreateInstance( type.GetElementType(), values.Length );
        
                for ( int i = 0; i < values.Length; i++ )
                {
                    array.SetValue( ReadPropertyFromJson( values[i], type.GetElementType() ), i );
                }
        
                return array;
            }
            else if ( type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) )
            {
                var values = token.Children().ToArray();
                var list = (IList)Activator.CreateInstance( type );    
                var elementTypes = type.GetGenericArguments()[0];
        
                foreach ( var valueToken in values )
                {

                    var itemTypeToken = valueToken[ ".Type" ];
                    if ( itemTypeToken != null )
                    {
                        var itemType = Type.GetType( itemTypeToken.Value<String>() );
                        list.Add( ReadPropertyFromJson( valueToken, itemType ) );
                    }
                    else
                    {
                        list.Add( ReadPropertyFromJson( valueToken, type.GetElementType() ) );
                    }
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

        
    }
}