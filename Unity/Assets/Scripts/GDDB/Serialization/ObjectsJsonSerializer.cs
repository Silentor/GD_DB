﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Object = System.Object;
using UnityEngine;

namespace GDDB.Serialization
{
    public class ObjectsJsonSerializer : ObjectsJsonCommon
    {
        public ObjectsJsonSerializer( )
        {
#if UNITY_2021_2_OR_NEWER
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
#endif
        }

        public void AddSerializer( TypeCustomSerializer serializer )
        {
            _serializers.Add( serializer.SerializedType, serializer );
        }

#if UNITY_EDITOR

        public JObject Serialize( GDObject @object, IGdAssetResolver assetResolver = null )
        {
            if ( !@object.EnabledObject )
                return null;

            _assetResolver = assetResolver ?? NullGdAssetResolver.Instance;
            
            if( @object is ISerializationCallbackReceiver serializationCallbackReceiver)
                serializationCallbackReceiver.OnBeforeSerialize();

            foreach ( var gdComponent in @object.Components )
                if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                    componentSerializationCallbackReceiver.OnBeforeSerialize();

            var result =  WriteGDObjectToJson( @object );                    

            Debug.Log( $"[{nameof(ObjectsJsonSerializer)}] Serialized gd object {@object.Name} to json, referenced {_assetResolver.Count} assets, used asset resolver {_assetResolver.GetType().Name}" );

            return result;
        }
        
#endif

        
        private          IGdAssetResolver                        _assetResolver;

#if UNITY_EDITOR

        private JObject  WriteGDObjectToJson( GDObject obj )
        {
            try
            {
                var result = new JObject();
                result.Add( ".Name", obj.name );
                var type = obj.GetType();
                if( obj.GetType() != typeof(GDObject) )
                    result.Add( ".Type", type.Assembly == typeof(GDObject).Assembly ? type.FullName : type.AssemblyQualifiedName );
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
                //value = new JObject();
                //result.Add( ".Value", value );
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

        // private JSONObject WriteReferenceToJson( Type propertyType, Guid guid )
        // {
        //     var result = new JSONObject();
        //     result.Add( ".Ref", guid.ToString() );
        //     return result;
        // }


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
                        writer.Add( field.Name, property );
                    }
                    else
                    {
                        var property = WriteNullPropertyToJson( field.Name, field.FieldType );
                        writer.Add( field.Name, property );
                    }
                }
            }
        }

       
        private JToken WritePropertyToJson( String propertyName, Type propertyType, Object value, Type valueType )
        {
            try
            {
                return WriteSomethingToJson( propertyType, value, valueType );
            }
            catch ( Exception e )
            {
                throw new JsonPropertyException( propertyName, null, $"Error writing property {propertyName} of type {propertyType} value {value} ", e );
            }
            
        }

        private JToken WriteNullPropertyToJson( String propertyName, Type propertyType )
        {
            return WriteSomethingNullToJson( propertyType );
        }

        private JToken WriteSomethingToJson(Type propertyType, Object value, Type valueType )
        {
            if ( valueType == typeof(Char) )
            {
                return new JValue( Convert.ToChar( value, CultureInfo.InvariantCulture ) );
            }
            else if ( valueType == typeof(String) )
            {
                return new JValue( (String)value );
            }
            else if ( valueType.IsPrimitive)
            {
                if ( valueType == typeof(Boolean) )
                    return new JValue( (Boolean)value );
                if ( valueType == typeof(Single) || valueType == typeof(Double) )
                    return new JValue( Convert.ToDouble(value) );
                else if ( valueType == typeof(UInt64) )
                    return new JValue( (UInt64)value );
                else
                    return new JValue( Convert.ToInt64(value) );
            }
            else if ( valueType.IsEnum )
            {
                return new JValue( Convert.ToString( value, CultureInfo.InvariantCulture ) );
            }
            else if( valueType.IsArray)
            {
                var elementType = valueType.GetElementType();
                return WriteCollectionToJson( elementType, (IList)value );
            }
            else if( valueType.IsGenericType && valueType.GetGenericTypeDefinition() ==  typeof(List<>))
            {
                var elementType = valueType.GetGenericArguments()[0];
                return WriteCollectionToJson( elementType, (IList)value );
            }
            else if( typeof(GDObject).IsAssignableFrom( valueType) )
            {
                var gdObject = (GDObject)value;
                return new JValue( gdObject.Guid.ToString("D") );
                //return WriteReferenceToJson( propertyType, gdObject.Guid );
            }
            else if ( _serializers.TryGetValue( propertyType, out var serializer ) )
            {
                return serializer.Serialize( value );
            }
            else if ( value is UnityEngine.Object unityObj && UnityEditor.AssetDatabase.Contains( unityObj ) )      //Write Unity asset reference
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
                //Create and serialize empty object (Unity-way compatibility)       //todo consider write JSON null, restore object at loading
                var emptyObject  = CreateEmptyObject( propertyType );
                var jEmptyObject = emptyObject != null ? WriteObjectToJson( propertyType, emptyObject ) : new JObject();
                return jEmptyObject;
            }
        }

        private JArray WriteCollectionToJson( Type elementType, IList collection )
        {
            var result = new JArray();
            for ( int i = 0; i < collection.Count; i++ )
            {
                var obj = collection[i];
                if( obj != null )
                    result.Add( WriteSomethingToJson( elementType, obj, obj.GetType() ) );
                else
                    result.Add( WriteSomethingNullToJson( elementType ) );
            }

            return result;
        }

        private JObject WriteUnityObjectToJson( UnityEngine.Object unityAsset )
        {
            JObject result = null;
            if ( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( unityAsset, out var guid, out long localId ))
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
    }
}