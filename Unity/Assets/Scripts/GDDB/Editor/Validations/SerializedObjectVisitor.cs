using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor.Validations
{
    /// <summary>
    /// Iterate serialized object using serProp.NextVisible() but also provide you with exact FieldInfo
    /// </summary>
    public class SerializedObjectVisitor
    {
        private readonly Dictionary<Type, TypeFieldsCache> _typeFieldsCache = new ();

        public void Iterate( SerializedObject obj )
        {
            var prop = obj.GetIterator();
            IterateObject( prop, obj.targetObject.GetType() );
        }

        protected EVisitResult IterateObject( SerializedProperty objProp, Type objType )
        {
            var depth         = objProp.depth;
            var enterChildren = true;
            var typeCache     = GetCachedType( objType );
            var i             = 0;
            while ( objProp.NextVisible( enterChildren ) && objProp.depth > depth )
            {
                enterChildren = false;
                var fieldInfo =  typeCache.GetFieldInfo( objProp, i++ );
                if( fieldInfo.Item1 == null )           //Some Unity internal field
                    continue;

                var visitResult = VisitProperty( objProp, fieldInfo.Item1 );
                if( visitResult == EVisitResult.SkipChildren )
                    continue;
                else if ( visitResult == EVisitResult.Stop )
                    return EVisitResult.Stop;

                //Manage field with complex type
                switch ( fieldInfo.Item2 )
                {
                    case EFieldKind.EmbeddedObject:
                        if( IterateObject( objProp.Copy(), fieldInfo.Item1.FieldType ) == EVisitResult.Stop )
                            return EVisitResult.Stop;
                        break;
                    case EFieldKind.Collection:
                        if( IterateCollection( objProp.Copy(), fieldInfo.Item1 ) == EVisitResult.Stop )
                            return EVisitResult.Stop;
                        break;
                    case EFieldKind.ManagedReference:
                        if( IterateObject( objProp.Copy(), objProp.managedReferenceValue.GetType() ) == EVisitResult.Stop )
                            return EVisitResult.Stop;
                        break;
                }
            }

            return EVisitResult.Continue;
        }

        protected EVisitResult IterateCollection( SerializedProperty collectionProp, FieldInfo fieldInfo )
        {
            EFieldKind? elementKind = null;
            Type        elementType = null;

            for ( int i = 0; i < collectionProp.arraySize; i++ )
            {
                var childProp     = collectionProp.GetArrayElementAtIndex( i );
                var visitResult = VisitProperty( childProp, fieldInfo );
                if( visitResult == EVisitResult.SkipChildren )
                    continue;
                else if ( visitResult == EVisitResult.Stop )
                    return EVisitResult.Stop;

                //Manage elements with complex type
                elementKind ??= TypeFieldsCache.GetFieldType( childProp);
                elementType ??= GetCollectionElementType( fieldInfo.FieldType );
                switch ( elementKind.Value )
                {
                    case EFieldKind.EmbeddedObject:
                        if(IterateObject( childProp, elementType ) == EVisitResult.Stop )
                            return EVisitResult.Stop;
                        break;
                    // case EFieldKind.Collection:                  //Unity does not support nested collections serialization
                    //     if( IterateCollection( childProp, fieldInfo ) == EVisitResult.Stop )
                    //         return EVisitResult.Stop;
                    //     break;
                    case EFieldKind.ManagedReference:
                        if( IterateObject( childProp, childProp.managedReferenceValue.GetType() ) == EVisitResult.Stop )
                            return EVisitResult.Stop;
                        break;
                }
            }

            return EVisitResult.Continue;
        }

        private Type GetCollectionElementType( Type collectionType )
        {
            if( collectionType.IsArray )
                return collectionType.GetElementType();

            if( collectionType.IsGenericType )                           //For Unity its always List<>
                return collectionType.GetGenericArguments()[0];

            return null;
        }

        protected virtual EVisitResult VisitProperty( SerializedProperty prop, FieldInfo fieldInfo )
        {
            Debug.Log( $"{prop.propertyPath} ({prop.name}), depth {prop.depth}, proptype {prop.propertyType}, field {fieldInfo}, value {(!prop.isArray ? prop.boxedValue : "")}" );
            return EVisitResult.Continue;
        }

        private TypeFieldsCache GetCachedType( Type type )
        {
            if( !_typeFieldsCache.TryGetValue( type, out var cache ) )
            {
                cache = new TypeFieldsCache ( type );
                _typeFieldsCache[type] = cache;
            }

            return cache;
        }

        private readonly struct TypeFieldsCache
        {
            private static readonly BindingFlags    BindingFlags;

            public readonly         Type            Type;
            public readonly         List<(FieldInfo, EFieldKind)> Fields;

            public TypeFieldsCache(Type type ) : this()
            {
                Type   = type;
                Fields = new List<(FieldInfo, EFieldKind)>();
            }

            static TypeFieldsCache( )
            {
                BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            }

            public (FieldInfo, EFieldKind) GetFieldInfo( SerializedProperty serProp, Int32 fieldIndex )
            {
                if( Fields.Count <= fieldIndex )
                {
                    var field  = Type.GetField( serProp.name, BindingFlags );
                    var type   = GetFieldType( serProp );
                    var result = (field, type);
                    Fields.Add( result );
                    return result;
                }

                return Fields[fieldIndex];
            }

            public static EFieldKind GetFieldType( SerializedProperty serProp )
            {
                var serPropType = serProp.propertyType;
                if( serPropType == SerializedPropertyType.Generic )
                    if( serProp.isArray )
                        return EFieldKind.Collection;
                    else
                        return EFieldKind.EmbeddedObject;

                if ( serPropType == SerializedPropertyType.ManagedReference )
                    return EFieldKind.ManagedReference;

                return EFieldKind.Default;
            }
        }

        private enum EFieldKind
        {
            Default,
            EmbeddedObject,
            Collection,
            ManagedReference,
        }

        protected enum EVisitResult
        {
            Continue,
            SkipChildren,
            Stop
        }
    }
}