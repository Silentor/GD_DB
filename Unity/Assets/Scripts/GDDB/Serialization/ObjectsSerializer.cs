using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Serialization
{
    public partial class ObjectsSerializer
    {
        public ObjectsSerializer( )
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

#if UNITY_EDITOR
        public JArray Serialize( IEnumerable<GDObject> objects, IGdAssetResolver assetResolver = null )
        {
            JArray resulObjects = new JArray();
            _assetResolver = assetResolver ?? NullGdAssetResolver.Instance;

            foreach ( var gdObj in objects )
            {
                if( gdObj is ISerializationCallbackReceiver serializationCallbackReceiver)
                    serializationCallbackReceiver.OnBeforeSerialize();

                foreach ( var gdComponent in gdObj.Components )
                    if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                        componentSerializationCallbackReceiver.OnBeforeSerialize();

                resulObjects.Add(  WriteGDObjectToJson( gdObj ) );                    
            }

            Debug.Log( $"[{nameof(ObjectsSerializer)}] Serialized {resulObjects.Count} gd objects to json, Referenced {_assetResolver.Count} assets, used asset resolver {_assetResolver.GetType().Name}" );

            return resulObjects;
        }
        
#endif

        private readonly Dictionary<Type, GdJsonCustomSerializer> _serializers = new();
        private          IGdAssetResolver                        _assetResolver;

#if UNITY_EDITOR

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
                _assetResolver.AddAsset( unityAsset, guid, localId );
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

#endif
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