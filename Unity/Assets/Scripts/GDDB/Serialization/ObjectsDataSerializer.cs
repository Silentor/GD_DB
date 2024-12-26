using System;
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
    public class ObjectsDataSerializer : ObjectsJsonCommon
    {
        private readonly WriterBase _writer;

        public ObjectsDataSerializer( WriterBase writer )
        {
            _writer = writer;

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

        public void AddSerializer<T>( TypeCustomSerializer<T> serializer )
        {
            _serializers.Add( serializer.SerializedType, serializer );
        }

#if UNITY_EDITOR

        public void Serialize( GDObject @object,IGdAssetResolver assetResolver = null )
        {
            if ( !@object.EnabledObject )
                return;

            _assetResolver = assetResolver ?? NullGdAssetResolver.Instance;
            
            if( @object is ISerializationCallbackReceiver serializationCallbackReceiver)
                serializationCallbackReceiver.OnBeforeSerialize();

            foreach ( var gdComponent in @object.Components )
                if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                    componentSerializationCallbackReceiver.OnBeforeSerialize();

            WriteGDObjectToJson( @object, _writer );                    

            Debug.Log( $"[{nameof(ObjectsDataSerializer)}] Serialized gd object {@object.Name} to json, referenced {_assetResolver.Count} assets, used asset resolver {_assetResolver.GetType().Name}" );
        }
        
#endif

        
        private          IGdAssetResolver                        _assetResolver;

#if UNITY_EDITOR

        private void  WriteGDObjectToJson( GDObject obj, WriterBase writer )
        {
            try
            {
                writer.WriteStartObject();
                writer.WritePropertyName( ".Name" );
                writer.WriteValue( obj.name );
                var type = obj.GetType();
                if ( obj.GetType() != typeof(GDObject) )
                {
                    writer.WritePropertyName( ".Type" );
                    writer.WriteValue( type.Assembly == typeof(GDObject).Assembly ? type.FullName : type.AssemblyQualifiedName );
                }
                writer.WritePropertyName( ".Ref" );
                writer.WriteValue( obj.Guid.ToString("D") );
                if ( !obj.EnabledObject )
                {
                    writer.WritePropertyName( ".Enabled" );
                    writer.WriteValue( false );
                }

                writer.WritePropertyName( ".Components" );
                writer.WriteStartArray();
                foreach ( var gdComponent in obj.Components )
                {
                    if( gdComponent == null || gdComponent.GetType().IsAbstract )       //Seems like missed class component
                        continue;

                    WriteObjectToJson( typeof(GDComponent), gdComponent, writer ) ;
                }
                writer.WriteEndArray();

                WriteObjectContent( type, obj, writer );

                writer.WriteEndObject();
            }
            catch ( Exception e )
            {
                throw new ReaderObjectException( obj.name, obj.GetType(), null, $"Error writing object {obj.name} of type {obj.GetType()}", e );
            }
        }

        private void WriteObjectToJson( Type propertyType, Object obj, WriterBase writer )
        {
            writer.WriteStartObject();
            var actualType = obj.GetType();

            //Check for polymorphic object
            if ( propertyType != actualType )
            {
                writer.WritePropertyName( ".Type" );
                writer.WriteValue( actualType.Assembly == GetType().Assembly ? actualType.FullName : actualType.AssemblyQualifiedName );
            }

            WriteObjectContent( actualType, obj, writer );
        }

        // private JSONObject WriteReferenceToJson( Type propertyType, Guid guid )
        // {
        //     var result = new JSONObject();
        //     result.Add( ".Ref", guid.ToString() );
        //     return result;
        // }


        private void WriteObjectContent( Type actualType, Object obj, WriterBase writer )
        {
            foreach (var field in actualType.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ))
            {
                if( IsFieldSerializable( field ))
                {
                    var value = field.GetValue(obj);
                    if ( value != null )
                    {
                        WritePropertyToJson( field.Name, field.FieldType, value, value.GetType(), writer );
                    }
                    else
                    {
                        WriteNullPropertyToJson( field.Name, field.FieldType, writer );
                    }
                }
            }
        }

       
        private void WritePropertyToJson( String propertyName, Type propertyType, Object value, Type valueType, WriterBase writer )
        {
            try
            {
                writer.WritePropertyName( propertyName );
                WriteSomethingToJson( propertyType, value, valueType, writer );
            }
            catch ( Exception e )
            {
                throw new ReaderPropertyException( propertyName, null, $"Error writing property {propertyName} of type {propertyType} value {value} ", e );
            }
            
        }

        private void WriteNullPropertyToJson( String propertyName, Type propertyType, WriterBase writer )
        {
            writer.WritePropertyName( propertyName );
            WriteSomethingNullToJson( propertyType, writer );
        }

        private void WriteSomethingToJson(Type propertyType, Object value, Type valueType, WriterBase writer )
        {
            if ( valueType == typeof(Char) )
            {
                writer.WriteValue( Convert.ToString( value, CultureInfo.InvariantCulture ) );
            }
            else if ( valueType == typeof(String) )
            {
                writer.WriteValue( (String)value );
            }
            else if ( valueType.IsPrimitive)
            {
                if ( valueType == typeof(Boolean) )
                    writer.WriteValue( (Boolean)value );
                if ( valueType == typeof(Single) )
                    writer.WriteValue( Convert.ToSingle(value) );
                if ( valueType == typeof(Double) )
                    writer.WriteValue( Convert.ToDouble(value) );
                else if ( valueType == typeof(UInt64) )
                    writer.WriteValue( (UInt64)value );
                else
                    writer.WriteValue( Convert.ToInt64(value) );
            }
            else if ( valueType.IsEnum )
            {
                writer.WriteValue( Convert.ToString( value, CultureInfo.InvariantCulture ) );       //TODO make WriterBase.WriteEnum()
            }
            else if( valueType.IsArray)
            {
                var elementType = valueType.GetElementType();
                WriteCollectionToJson( elementType, (IList)value, writer );
            }
            else if( valueType.IsGenericType && valueType.GetGenericTypeDefinition() ==  typeof(List<>))
            {
                var elementType = valueType.GetGenericArguments()[0];
                WriteCollectionToJson( elementType, (IList)value, writer );
            }
            else if( typeof(GDObject).IsAssignableFrom( valueType) )
            {
                writer.WriteValue( ((GDObject)value).Guid.ToString("D") );                  //TODO make WriterBase.WriteGuidValue()
            }
            else if ( _serializers.TryGetValue( propertyType, out var serializer ) )
            {
                serializer.Serialize( writer, value );
            }
            else if ( value is UnityEngine.Object unityObj && UnityEditor.AssetDatabase.Contains( unityObj ) )      //Write Unity asset reference
            {
                WriteUnityObjectToJson( unityObj, writer );
            }
            else
            {
                WriteObjectToJson( propertyType, value, writer );
            }
        }

        private void WriteSomethingNullToJson(Type propertyType, WriterBase writer )
        {
            if( propertyType == typeof(String) )
                writer.WriteValue( String.Empty );                  //todo consider write null, save some bytes for binary mode
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>)) )
            {
                writer.WriteStartArray();                           //todo consider write null, save some bytes for binary mode
                writer.WriteEndArray();
            }
            else
            {
                //Create and serialize empty object (Unity-way compatibility)       //todo consider write JSON null, restore object at loading
                var emptyObject  = CreateEmptyObject( propertyType );
                if ( emptyObject != null )
                    WriteObjectToJson( propertyType, emptyObject, writer );
                else 
                    writer.WriteNullValue();
            }
        }

        private void WriteCollectionToJson( Type elementType, IList collection, WriterBase writer )
        {
            writer.WriteStartArray();

            for ( int i = 0; i < collection.Count; i++ )
            {
                var obj = collection[i];
                if( obj != null )
                    WriteSomethingToJson( elementType, obj, obj.GetType(), writer );
                else
                    WriteSomethingNullToJson( elementType, writer );
            }

            writer.WriteEndArray();
        }

        private void WriteUnityObjectToJson( UnityEngine.Object unityAsset, WriterBase writer )
        {                              
            writer.WriteStartObject();

            if ( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( unityAsset, out var guid, out long localId ))
            {
                writer.WritePropertyName( ".Ref" );
                writer.WriteValue( guid );
                writer.WritePropertyName( ".Id" );
                writer.WriteValue( localId );
                _assetResolver.AddAsset( unityAsset, guid, localId );
            }
            else
            {
                writer.WritePropertyName( ".Error" );
                writer.WriteValue( $"Error serializing {unityAsset.name} ({unityAsset.GetType()}), can not find asset guid" );
                Debug.LogError( $"Error serializing {unityAsset.name} ({unityAsset.GetType()}), can not find asset guid" );
            }

            writer.WriteEndObject();
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