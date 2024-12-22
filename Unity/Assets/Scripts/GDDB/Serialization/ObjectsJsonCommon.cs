using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GDDB.Serialization
{
    public class ObjectsJsonCommon
    {
        protected readonly Dictionary<Type, TypeCustomSerializer> _serializers       = new();
        private readonly   Dictionary<Type, Boolean>              _isTypeSerializableCache = new();

        protected Boolean IsFieldSerializable( FieldInfo field )
        {
            if ( field.IsInitOnly || field.IsLiteral || field.IsStatic || field.IsNotSerialized )
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
            if ( !IsTypeSerializable( fieldType ) && !field.IsDefined( typeof(SerializeReference) ) )
                return false;

            return true;
        }

        protected Boolean IsTypeSerializable( Type type )
        {
            if ( !_isTypeSerializableCache.TryGetValue( type, out var result ) )
            {
                result = IsTypeSerializableInternal( type );
                _isTypeSerializableCache.Add( type, result );
                return result;
            }

            return result;
        }

        protected Boolean IsTypeSerializableInternal( Type type )
        {

            //Fast pass
            if ( type == typeof(String) || type.IsEnum || _serializers.ContainsKey( type ) )
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
            if ( type.IsClass && !(type.IsDefined( typeof(SerializableAttribute ) ) || isUnityAssetType ) )
                return false;

            if ( type.IsArray )
                return IsTypeSerializable( type.GetElementType() );

            if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) )
                return IsTypeSerializable( type.GetGenericArguments()[ 0 ] );

            if ( !isUnityAssetType )
            {
                //Check serializable fields for serialization support
                var fields = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
                if ( !fields.Any( IsFieldSerializable ) )
                    return false;
            }

            return true;
        }
    }
}