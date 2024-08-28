using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    /// <summary>
    /// Helper class to work with types of GDObjects without creating entire GDDB
    /// </summary>
    public class GDObjectsFinder
    {
        public readonly List<GDObject>   GDObjects = new();
        public readonly List<GDObject>   GDTypedObjects = new();
        public readonly List<GDRoot>     GDRoots = new();

        public GDObjectsFinder( )
        {
            var timer = new System.Diagnostics.Stopwatch();
            LoadGDObjects();
            //Debug.Log( $"[GDObjectsFinder] Loaded all gdobjects at {timer.Elapsed.TotalMilliseconds:N3} msec" );
        }

        public void Reload( )
        {
            LoadGDObjects();
        }

        public Boolean IsDuplicatedType( GdType type, GDTypeHierarchy typeHierarchy, out Int32 count )
        {
            count = 0;
            if ( type == default )
            {
                return false;
            }

            var metadata = typeHierarchy.GetMetadataOf( type );
            return IsDuplicatedType( type, metadata, out count );
        }

        public Boolean IsDuplicatedType( GdType type, GDTypeHierarchy.GdTypeMetadata metadata, out Int32 count )
        {
            count = 0;
            if ( type == default )
            {
                return false;
            }

            for ( var i = 0; i < GDTypedObjects.Count; i++ )
            {
                if( GDTypedObjects[i].EnabledObject == false )
                    continue;

                if ( metadata.IsTypesEqual( type, GDTypedObjects[ i ].Type ) )
                {
                    count++;
                }                
            }

            return count > 1;
        }

        public Boolean FindFreeType( GdType fromType, GDTypeHierarchy typeHierarchy, out GdType result )
        {
            if ( fromType == default )
            {
                result = default;
                return false;
            }

            var metadata = typeHierarchy.GetMetadataOf( fromType );
            return FindFreeType( fromType, metadata, out result );
        }

        /// <summary>
        /// Find free 4th category for given type
        /// </summary>
        /// <param name="fromType"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public Boolean FindFreeType( GdType fromType, GDTypeHierarchy.GdTypeMetadata metadata, out GdType result )
        {
            if ( fromType == default )
            {
                result = default;
                return false;
            }


            var    valueCategory = metadata.Categories.Last();
            var    value         = valueCategory.GetValue( fromType.Data );

            for ( var newValue = value + 1; newValue < valueCategory.MaxValue; newValue++ )
            {
                if ( IsValidAndUnique( newValue, out result ) )
                    return true;
            }

            for ( var newValue = valueCategory.MinValue; newValue < value; newValue++ )     
            {
                if ( IsValidAndUnique( newValue, out result ) )
                    return true;
            }

            result = default;
            return false;

            Boolean IsValidAndUnique( Int32 newValue, out GdType newType )
            {
                if ( valueCategory.IsCorrectValue( newValue ) )
                {
                    var checkType = fromType;
                    checkType.Data = valueCategory.SetValue( checkType.Data, newValue );
                    if ( checkType != default && checkType != fromType && !GDTypedObjects.Exists( g => metadata.IsTypesEqual( g.Type, checkType ) ) )
                    {
                        newType = checkType;
                        return true;
                    }
                }

                newType = default;
                return false;
            }
        }

        private void LoadGDObjects( )
        {
            GDObjects.Clear();
            GDTypedObjects.Clear();
            GDRoots.Clear();

            var gdObjectGuids = AssetDatabase.FindAssets( "t:GDObject" );
            foreach ( var gdObjectGuid in gdObjectGuids )
            {
                var path              = AssetDatabase.GUIDToAssetPath( gdObjectGuid );
                var gdObject = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                if ( gdObject )
                {
                    GDObjects.Add( gdObject );
                    if( gdObject.Type != default )
                        GDTypedObjects.Add( gdObject );
                    if( gdObject is GDRoot gdroot )
                        GDRoots.Add( gdroot );
                }
            }
        }
    }
}