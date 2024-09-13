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
        

        public GDJson( )
        {
            AddSerializer( new Vector3Serializer() );
            AddSerializer( new Vector3IntSerializer() );
            AddSerializer( new Vector2Serializer() );
            AddSerializer( new Vector2IntSerializer() );
            AddSerializer( new QuaternionSerializer() );
            AddSerializer( new RectSerializer() );
            AddSerializer( new BoundsSerializer() );
            AddSerializer( new Color32Serializer() );
            AddSerializer( new ColorSerializer() );
            AddSerializer( new AnimationCurveSerializer() );
        }

        public void AddSerializer( GdJsonCustomSerializer serializer )
        {
            _serializers.Add( serializer.SerializedType, serializer );
        }

        public String GDToJson( IEnumerable<GDObject> objects, GdAssetReference assetReference = null )
        {
            JArray resulObjects = new JArray();
            _assetReference = assetReference ? assetReference : ScriptableObject.CreateInstance<GdAssetReference>();

            foreach ( var gdObj in objects )
            {
                if( gdObj is ISerializationCallbackReceiver serializationCallbackReceiver)
                    serializationCallbackReceiver.OnBeforeSerialize();

                foreach ( var gdComponent in gdObj.Components )
                    if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                        componentSerializationCallbackReceiver.OnBeforeSerialize();

                resulObjects.Add(  WriteGDObjectToJson( gdObj ) );                    
            }

            var result = resulObjects.ToString();
            Debug.Log( $"Written {objects.Count()} gd objects to json string {result.Length} symbols. Referenced {_assetReference.Assets.Count} assets, resolver {_assetReference.GetType().Name}" );

            return result;
        }

        private readonly Dictionary<Type, GdJsonCustomSerializer> _serializers = new();
        private readonly List<GDObject>                           _headers     = new();
        private          GdAssetReference                         _assetReference;

        private void WriteNullPropertyToJson( String name, JsonWriter writer )
        {
            writer.WritePropertyName(name);
            writer.WriteNull();
        }

        

        private JObject  WriteGDObjectToJson( GDObject obj )
        {
            try
            {
                var result = new JObject();
                result.Add( ".Name", obj.name );
                var type = obj.GetType();
                result.Add( ".Type", type.Assembly == GetType().Assembly ? type.FullName : type.AssemblyQualifiedName );
                result.Add( ".Ref",  obj.Guid.ToString("D") );
                if( !obj.EnabledObject )
                    result.Add( ".Enabled", false );

                var componentsArray = new JArray();
                result.Add( ".Components", componentsArray );
                foreach ( var gdComponent in obj.Components )
                {
                    if( gdComponent == null || gdComponent.GetType().IsAbstract )       //Seems like missed class component
                        continue;
                    componentsArray.Add( WriteObjectToJson( typeof(GDComponent), gdComponent ) );
                }

                WriteObjectContent( type, obj, result );

                return result;
            }
            catch ( Exception e )
            {
                throw new JsonObjectException( obj.name, obj.GetType(), null, $"Error writing object {obj.name} of type {obj.GetType()}", e );
            }
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

        private JObject WriteReferenceToJson( Type propertyType, Guid guid )
        {
            var result = new JObject();
            result.Add( ".Ref", guid.ToString() );
            return result;
        }


        private void WriteObjectContent( Type actualType, Object obj, JObject writer )
        {
            foreach (var field in actualType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( IsFieldSerializable( field ))
                {
                    var value = field.GetValue(obj);
                    if ( value != null )
                    {
                        var property = WritePropertyToJson( field.Name, field.FieldType, value, value.GetType() );
                        writer.Add( property );
                    }
                    else
                    {
                        var property = WriteNullPropertyToJson( field.Name, field.FieldType );
                        writer.Add( property );
                    }
                }
            }
        }

       
        private JProperty WritePropertyToJson( String propertyName, Type propertyType, Object value, Type valueType )
        {
            try
            {
                var result = new JProperty( propertyName );
                result.Value = WriteSomethingToJson( propertyType, value, valueType );
                return result;
            }
            catch ( Exception e )
            {
                throw new JsonPropertyException( propertyName, null, $"Error writing property {propertyName} of type {propertyType} value {value} ", e );
            }
            
        }

        private JProperty WriteNullPropertyToJson( String propertyName, Type propertyType )
        {
            var result = new JProperty( propertyName );
            result.Value = WriteSomethingNullToJson( propertyType );
            return result;
        }

        private JToken WriteSomethingToJson(Type propertyType, Object value, Type valueType )
        {
            if ( valueType == typeof(Char) )
            {
                return new JValue( Convert.ToUInt16( value) );
            }
            else if( valueType.IsPrimitive || valueType == typeof(String) )
            {
                return new JValue( value );
            }
            else if ( valueType.IsEnum )
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
            else if( typeof(GDObject).IsAssignableFrom( valueType) )
            {
                var gdObject = (GDObject)value;
                return WriteReferenceToJson( propertyType, gdObject.Guid );
            }
            else if ( _serializers.TryGetValue( propertyType, out var serializer ) )
            {
                return serializer.Serialize( value );
            }
            else if ( value is UnityEngine.Object unityObj && AssetDatabase.Contains( unityObj ) )      //Write Unity asset reference
            {
                return WriteUnityObjectToJson( unityObj );
            }
            else
            {
                return WriteObjectToJson( propertyType, value );
            }
        }

        private JToken WriteSomethingNullToJson(Type propertyType )
        {
            if( propertyType == typeof(String) )
                return new JValue( String.Empty );
            else if( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>)))
                return new JArray();
            else
            {
                //Create and serialize empty object
                var emptyObject  = CreateEmptyObject( propertyType );
                var jEmptyObject = emptyObject != null ? WriteObjectToJson( propertyType, emptyObject ) : new JObject();
                return jEmptyObject;
            }
        }

        private JArray WriteCollectionToJson( Type elementType, IEnumerable collection )
        {
            var result = new JArray();
            foreach (var obj in collection)
            {
                if( obj != null )
                    result.Add( WriteSomethingToJson( elementType, obj, obj.GetType() ) );
                else
                    result.Add( WriteSomethingNullToJson( elementType ) );
            }

            return result;
        }

        private JObject WriteUnityObjectToJson( UnityEngine.Object unityAsset )
        {
            JObject result = new JObject();
            if ( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( unityAsset, out var guid, out long localId ))
            {
                result = new JObject();
                result.Add( ".Ref", guid );
                result.Add( ".Id", localId );
                _assetReference.AddAsset( unityAsset, guid, localId );
            }
            else
            {
                result.Add( ".Error", $"Error serializing {unityAsset.name} ({unityAsset.GetType()}), can not find asset guid" );
                Debug.LogError( $"Error serializing {unityAsset.name} ({unityAsset.GetType()}), can not find asset guid" );
            }

            return result;
        }                                

        private Object CreateEmptyObject( Type type )
        {
            if( type.IsAbstract || type.IsInterface )
            {
                Debug.LogError( $"Can not create empty object for abstract or interface type {type} return null" );
                return null;
            }

            var defaultConstructor = type.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {type} return null" );
                return null;
            }

            //Null fields will be 'inited' by serializer
            var result = defaultConstructor.Invoke( Array.Empty<Object>() );
            return result;
        }


    #region JSON-LINQ reader

       public List<GDObject> JsonToGD( String json, GdAssetReference assetResolver = null )
        {
            _headers.Clear();
            _assetReference = assetResolver ? assetResolver : ScriptableObject.CreateInstance<GdAssetReference>();

            using (var reader = new StringReader( json ) )
            {
                var o       = (JArray)JToken.ReadFrom(new JsonTextReader(reader));
                var content = o.Children<JObject>().ToArray();
                var result  = new List<GDObject>();
        
                //Read GDObject headers
                foreach ( var gdObject in content )
                {
                    var objHeader = ReadGDObjectHeaderFromJson( gdObject );
                    _headers.Add( objHeader );
                }
        
                //Read GDObject content (and resolve references)
                for ( var i = 0; i < content.Length; i++ )
                {
                    result.Add( ReadGDObjectContentFromJson( content[i], _headers[i] ) );                    
                }

                foreach ( var gdObject in result )
                {
                    if( gdObject is ISerializationCallbackReceiver serializationCallbackReceiver)
                        serializationCallbackReceiver.OnAfterDeserialize();

                    foreach ( var gdComponent in gdObject.Components )
                        if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                            componentSerializationCallbackReceiver.OnAfterDeserialize();

                }

                return result;
            }
        }

        private GDObject ReadGDObjectHeaderFromJson( JObject gdObjToken )
        {
            var name = gdObjToken[".Name"].Value<String>();
            var type = Type.GetType( gdObjToken[".Type"].Value<String>() );
            var guid = Guid.ParseExact( gdObjToken[".Ref"].Value<String>(), "D" );
            var enabled = gdObjToken[".Enabled"]?.Value<Boolean>() ?? true;
            var obj  = GDObject.CreateInstance( type ).SetGuid( guid );
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.name      = name;
            obj.EnabledObject   = enabled;
        
            return obj;
        }
        

        private GDObject ReadGDObjectContentFromJson( JObject gdObjToken, GDObject obj )
        {
            try
            {
                //Components should be read from reserved property
                var componentsProp = gdObjToken[".Components"] as JArray;
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
                        throw new JsonComponentException( i, componentjToken, $"Error reading component {i} from jtoken {componentjToken} of GDObject {obj.Name}({obj.Guid})", e );
                    }
                }

                ReadContentFromJson( gdObjToken, obj );
                return obj;
            }
            catch ( Exception e )
            {
                throw new JsonObjectException( obj.Name, obj.GetType(), gdObjToken, $"Error reading object {obj.Name} of type {obj.GetType()} from jobject {gdObjToken}", e );
            }
        }

        private Object ReadObjectFromJson( JObject jObject, Type propertyType )
        {
            if ( !jObject.HasValues )
                return null;

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
            var obj = defaultConstructor.Invoke( Array.Empty<Object>() );
        
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
                    if( valueToken == null )                        //Incorrect name
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
            else if( propertyType.IsPrimitive || propertyType == typeof(String) )
            {
                return Convert.ChangeType( value.Value<Object>(), propertyType );
            }
            else if ( propertyType.IsEnum )
            {
                return Convert.ChangeType( value.Value<Object>(), propertyType ); 
            }
            else if( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ))
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
            else if (typeof(UnityEngine.Object).IsAssignableFrom( propertyType ) )      //Read Unity asset by reference
            {
                if ( !value.HasValues )                  //Seems like missed reference or null reference
                    return null;

                var guid = value[".Ref"].Value<String>();
                var localId = value[".Id"].Value<long>();
                var asset = _assetReference.Assets.Find( a => a.Guid.Equals( guid ) && a.LocalId == localId )?.Asset;
                if( asset == null )
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

        private GDObject ReadGDObjectReferenceFromJson( JObject jObject )
        {
            var guid = Guid.Parse( jObject[".Ref"].Value<String>() );
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

        private Boolean IsFieldSerializable( FieldInfo field )
        {
            if( field.IsInitOnly || field.IsLiteral || field.IsStatic || field.IsNotSerialized )
                return false;

            if ( field.IsPrivate && !field.IsDefined( typeof(SerializeField), false ) )
                return false;

            if ( field.IsPublic && field.IsDefined( typeof(NonSerializedAttribute), false ) )
                return false;

            //Reserved fields for some types
            var declaredType = field.DeclaringType;
            if ( declaredType == typeof(GDObject) 
                 && (field.Name == nameof(GDObject.EnabledObject) || field.Name == nameof(GDObject.Components)) )
                return false;

            var fieldType = field.FieldType;
            if ( !IsTypeSerializable( fieldType ) && !field.IsDefined( typeof(SerializeReference) ))
                return false;

            return true;
        }

        private Boolean IsTypeSerializable( Type type )
        {
            //Fast pass
            if( type == typeof(String) || type.IsEnum || _serializers.ContainsKey( type ) )
                return true;

            //Unity serializer do not support all primitives
            if ( type.IsPrimitive )
                return !( type == typeof(Decimal) || type == typeof(IntPtr) || type == typeof(UIntPtr) );

            //Discard structures without Serializable attribute
            if ( !type.IsPrimitive && !type.IsEnum && type.IsValueType && !type.IsDefined( typeof(SerializableAttribute ) ) )
                return false;

            if ( type.IsAbstract || type.IsInterface )
                return false;

            //Discard classes without Serializable attribute or not derived from Unity Object (we try to serialize Unity Assets )
            var isUnityAssetType = typeof(UnityEngine.Object).IsAssignableFrom( type );
            if( type.IsClass && !(type.IsDefined( typeof(SerializableAttribute )) || isUnityAssetType )) 
                return false;

            if( type.IsArray )
                return IsTypeSerializable( type.GetElementType() );

            if( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) )
                return IsTypeSerializable( type.GetGenericArguments()[0] );

            if ( !isUnityAssetType )
            {
                //Check serializable fields for serialization support
                var fields = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
                if( !fields.Any( IsFieldSerializable ) )
                    return false;
            }

            return true;
        }
    }

    public class JsonPropertyException : Exception
    {
        public String PropertyName { get; }
        public JToken JToken { get; }

        public JsonPropertyException()
        {
            
        }

        public JsonPropertyException( String propertyName, JToken jToken, string message )
                : base(message)
        {
            PropertyName = propertyName;
            JToken  = jToken;
        }

        public JsonPropertyException(String propertyName, JToken jToken, string message, Exception inner )
                : base(message, inner)
        {
            PropertyName = propertyName;
            JToken  = jToken;
        }
    }

    public class JsonObjectException : Exception
    {
        public String ObjectName { get; }
        public Type   ObjectType { get; }
        public JToken JToken     { get; }

        public JsonObjectException()
        {
        }

        public JsonObjectException( String objectName, Type objectType, JToken jToken, string message)
                : base(message)
        {
            JToken     = jToken;
            ObjectName = objectName;
            ObjectType = objectType;
        }

        public JsonObjectException( String objectName, Type objectType, JToken jToken, string message, Exception inner)
                : base(message, inner)
        {
            ObjectName = objectName;
            ObjectType = objectType;
            JToken     = jToken;
        }
    }

    public class JsonComponentException : Exception
    {
        public Int32  ComponentIndex { get; }
        public JToken JToken         { get; }

        public JsonComponentException()
        {
        }

        public JsonComponentException( Int32 index, JToken jToken, string message)
                : base(message)
        {
            ComponentIndex = index;
            JToken         = jToken;
        }

        public JsonComponentException( Int32 index, JToken jToken, string message, Exception inner)
                : base(message, inner)
        {
            ComponentIndex = index;
            JToken         = jToken;
        }
    }
}