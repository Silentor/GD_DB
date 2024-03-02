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

        private void WritePropertyToJson( String name, Object value, Type type, JsonWriter writer )
        {
            writer.WritePropertyName(name);

            if( type.IsPrimitive || type == typeof(String) )
            {
                writer.WriteValue(value);
            }
            else if( type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) ))
            {
                WriteCollectionToJson( (IEnumerable)value, writer );
            }
            else
            {
                WriteObjectToJson( value, writer );
            }
        }

        private void WriteObjectToJson( GDObject obj, JsonWriter writer )
        {
            writer.WriteStartObject();

            writer.WritePropertyName( "_Name" );
            writer.WriteValue( obj.name );
            writer.WritePropertyName( "_Type" );
            var type = obj.GetType();
            writer.WriteValue( type.Assembly == GetType().Assembly ? type.FullName : type.AssemblyQualifiedName );

            foreach (var field in type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( field.IsPublic || field.IsDefined( typeof(SerializeField), false ))
                {
                    var value = field.GetValue(obj);
                    if( value == null )
                        WriteNullPropertyToJson( field.Name, writer );
                    else
                        WritePropertyToJson( field.Name, value, value.GetType(), writer );
                }
            }

            writer.WriteEndObject();
        }

        private void WriteObjectToJson( Object obj, JsonWriter writer )
        {
            writer.WriteStartObject();

            writer.WritePropertyName( "_Type" );
            var type = obj.GetType();
            writer.WriteValue( type.Assembly == GetType().Assembly ? type.FullName : type.AssemblyQualifiedName );

            foreach (var field in type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( field.IsPublic || field.IsDefined( typeof(SerializeField), false ))
                {
                    var value = field.GetValue(obj);
                    WritePropertyToJson( field.Name, value, value.GetType(), writer );
                }
            }

            writer.WriteEndObject();
        }

        private void WriteCollectionToJson( IEnumerable collection, JsonWriter writer )
        {
            writer.WriteStartArray();
            foreach (var obj in collection)
            {
                WriteObjectToJson(obj, writer);
            }
            writer.WriteEndArray();
        }

        public List<GDObject> JsonToGD( String json )
        {
            using (StringReader reader = new StringReader( json ))
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
            var type = Type.GetType( gdObjToken["_Type"].Value<String>() );
            var obj = (GDObject)ScriptableObject.CreateInstance( type );

            foreach (var field in type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( field.IsPublic || field.IsDefined( typeof(SerializeField), false ))
                {
                    var token = gdObjToken[field.Name];
                    if( token == null )
                        field.SetValue( obj, null );
                    else
                    {
                        try
                        {
                            field.SetValue( obj, ReadPropertyFromJson( token, field.FieldType ) );
                        }
                        catch ( Exception ex )  
                        {
                            Debug.LogError( $"Error settings field {field} from jtoken {token.Type} value {token}, exception: {ex}" );                            
                        }
                    }
                }
            }

            return obj;
        }

        private Object ReadPropertyFromJson( JToken token, Type type )
        {
            if( type.IsPrimitive || type == typeof(String) )
            {
                return Convert.ChangeType( ((JValue)token).Value, type );
            }
            else if( type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) ))
            {
                return ReadCollectionFromJson( (JArray)token, type );
            }
            else
            {
                return ReadObjectFromJson( (JObject)token );
            }
        }

        private Object ReadObjectFromJson( JObject token )
        {
            var type = Type.GetType( token["_Type"].Value<String>() );
            var defaultConstructor = type.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
                return null;
            var obj  = Activator.CreateInstance( type );

            foreach (var field in type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
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

                foreach ( var valueToken in values )
                {
                    var itemTypeToken = valueToken[ "_Type" ];
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
    }
}