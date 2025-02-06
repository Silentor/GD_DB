using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Object = System.Object;
using UnityEngine;

namespace GDDB.Serialization
{
    public class GDObjectSerializer : GDObjectSerializationCommon
    {
        public Int32 ObjectsWritten { get; private set; }

        private readonly WriterBase _writer;

        public GDObjectSerializer( WriterBase writer )
        {
            _writer = writer;

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

#if UNITY_EDITOR

        public void Serialize( ScriptableObject @object,IGdAssetResolver assetResolver = null )
        {
            var gdobject = @object as GDObject;
            if ( gdobject && !gdobject.EnabledObject )
                return;

            _assetResolver = assetResolver ?? NullGdAssetResolver.Instance;
            
            if( @object is ISerializationCallbackReceiver serializationCallbackReceiver)
                serializationCallbackReceiver.OnBeforeSerialize();

            if( gdobject )
            {
                foreach ( var gdComponent in gdobject.Components )
                    if ( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                        componentSerializationCallbackReceiver.OnBeforeSerialize();
            }
            WriteGDObject( @object, _writer );                    

            //Debug.Log( $"[{nameof(GDObjectSerializer)}] Serialized gd object {@object.Name} to {_writer.GetType().Name}, referenced {_assetResolver.Count} assets, used asset resolver {_assetResolver.GetType().Name}" );
        }
        
#endif

        
        private          IGdAssetResolver                        _assetResolver;

#if UNITY_EDITOR

        private void  WriteGDObject( ScriptableObject obj, WriterBase writer )
        {
            try
            {
                writer.WriteStartObject();
                writer.WritePropertyName( NameTag );
                writer.WriteValue( obj.name );
                var type = obj.GetType();

                if ( obj is GDObject gdobj )                                 //Serialize as GDObject ( with components )
                {
                    if ( type != typeof(GDObject) )
                    {
                        writer.WritePropertyName( TypeTag );
                        writer.WriteValue( type );
                    }
                    writer.WritePropertyName( IdTag );
                    writer.WriteValue( gdobj.Guid );
                    if ( !gdobj.EnabledObject )
                    {
                        writer.WritePropertyName( EnabledTag );
                        writer.WriteValue( false );
                    }

                    writer.WritePropertyName( ComponentsTag );
                    writer.WriteStartArray();
                    foreach ( var gdComponent in gdobj.Components )
                    {
                        if( gdComponent == null || gdComponent.GetType().IsAbstract )       //Seems like missed class component
                            continue;

                        WriteObject( typeof(GDComponent), gdComponent, writer ) ;
                    }
                    writer.WriteEndArray();
                }
                else                                                        //Serialize as plain ScriptableObject (no components, no embedded guid, no Enabled flag)
                {
                    writer.WritePropertyName( TypeTag );
                    writer.WriteValue( type );
                    writer.WritePropertyName( IdTag );
                    var guid = GetGuidFromSO( obj );
                    writer.WriteValue( guid );
                }

                WriteObjectContent( type, obj, writer );
                writer.WriteEndObject();

                ObjectsWritten++;
            }
            catch ( Exception e )
            {
                throw new WriterObjectException( obj.name, obj.GetType(), _writer, 
                        obj is GDObject gdobj 
                                ? $"Error writing object {obj.name} ({gdobj.Guid}) of type {obj.GetType()}"
                                : $"Error writing object {obj.name} of type {obj.GetType()}", 
                        e );
            }
        }

        private void WriteObject( Type propertyType, Object obj, WriterBase writer )
        {
            writer.WriteStartObject();
            var actualType = obj.GetType();

            //Check for polymorphic object
            if ( propertyType != actualType )
            {
                writer.WritePropertyName( TypeTag );
                writer.WriteValue( actualType );
            }

            WriteObjectContent( actualType, obj, writer );

            writer.WriteEndObject();
        }

        // private JSONObject WriteReferenceToJson( Type propertyType, Guid guid )
        // {
        //     var result = new JSONObject();
        //     result.Add( ".Ref", guid.ToString() );
        //     return result;
        // }


        private void WriteObjectContent( Type actualType, Object obj, WriterBase writer )
        {
            foreach (var field in GetSerializableFields( actualType ))
            {
                var value = field.GetValue(obj);
                if ( value != null )
                {
                    WriteProperty( field.Name, field.FieldType, value, value.GetType(), writer );
                }
                else
                {
                    WriteNullProperty( field.Name, field.FieldType, writer );
                }
            }
        }

       
        private void WriteProperty( String propertyName, Type propertyType, Object value, Type valueType, WriterBase writer )
        {
            try
            {
                writer.WritePropertyName( propertyName );
                WriteSomething( propertyType, value, valueType, writer );
            }
            catch ( Exception e )
            {
                throw new WriterPropertyException( propertyName, _writer, $"Error writing property {propertyName} of type {propertyType} value {value} ", e );
            }
            
        }

        private void WriteNullProperty( String propertyName, Type propertyType, WriterBase writer )
        {
            writer.WritePropertyName( propertyName );
            WriteSomethingNull( propertyType, writer );
        }

        private void WriteSomething(Type propertyType, Object value, Type valueType, WriterBase writer )
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
                else if ( valueType == typeof(Single) )
                    writer.WriteValue( (Single)value );
                else if ( valueType == typeof(Double) )
                    writer.WriteValue( (Double)value );
                else if ( valueType == typeof(UInt64) )
                    writer.WriteValue( (UInt64)value );
                else
                    writer.WriteValue( Convert.ToInt64(value) );
            }
            else if ( valueType.IsEnum )
            {
                writer.WriteValue( (Enum)value );
            }
            else if( valueType.IsArray)
            {
                var elementType = valueType.GetElementType();
                WriteCollection( elementType, (IList)value, writer );
            }
            else if( valueType.IsGenericType && valueType.GetGenericTypeDefinition() ==  typeof(List<>))
            {
                var elementType = valueType.GetGenericArguments()[0];
                WriteCollection( elementType, (IList)value, writer );
            }
            else if( typeof(ScriptableObject).IsAssignableFrom( valueType) )
            {
                if( value is GDObject gdObject )
                    writer.WriteValue( gdObject.Guid );
                else 
                {
                     var guid = GetGuidFromSO( (ScriptableObject)value );
                     writer.WriteValue( guid );
                }
            }
            else if ( _serializers.TryGetValue( propertyType, out var serializer ) )
            {
                serializer.Serialize( writer, value );
            }
            else if ( value is UnityEngine.Object unityObj && UnityEditor.AssetDatabase.Contains( unityObj ) )      //Write Unity asset reference
            {
                WriteUnityObject( unityObj, writer );
            }
            else
            {
                WriteObject( propertyType, value, writer );
            }
        }

        private void WriteSomethingNull(Type propertyType, WriterBase writer )
        {
            if( propertyType == typeof(String) )
                writer.WriteValue( String.Empty );                  //todo consider write null, save some bytes for binary mode
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>)) )
            {
                writer.WriteStartArray();                           //todo consider write null, save some bytes for binary mode
                writer.WriteEndArray();
            }
            else if ( typeof(UnityEngine.Object).IsAssignableFrom( propertyType ) )     //Reference-stored Unity objects
            {
                writer.WriteNullValue();
            }
            else 
            {
                //Create and serialize empty object (Unity-way compatibility)       //todo consider write JSON null, restore object at loading
                var emptyObject  = CreateEmptyObject( propertyType );
                if ( emptyObject != null )
                    WriteObject( propertyType, emptyObject, writer );
                else 
                    writer.WriteNullValue();
            }
        }

        private void WriteCollection( Type elementType, IList collection, WriterBase writer )
        {
            writer.WriteStartArray();

            for ( int i = 0; i < collection.Count; i++ )
            {
                var obj = collection[i];
                if( obj != null )
                    WriteSomething( elementType, obj, obj.GetType(), writer );
                else
                    WriteSomethingNull( elementType, writer );
            }

            writer.WriteEndArray();
        }

        private void WriteUnityObject( UnityEngine.Object unityAsset, WriterBase writer )
        {                              
            writer.WriteStartObject();

            if ( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( unityAsset, out var guid, out long localId ))
            {
                writer.WritePropertyName( IdTag );
                writer.WriteValue( guid );
                writer.WritePropertyName( LocalIdTag );
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

        private Guid GetGuidFromSO( ScriptableObject obj )
        {
            if ( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( obj, out var guidStr, out Int64 _ ) )
                return Guid.ParseExact( guidStr, "N" );
            else
            {
                //Probably runtime created for test purposes, so create runtime stable guid
                var guid = new Guid( obj.GetInstanceID(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 );
                return guid;
            }
        }

#endif
    }
}