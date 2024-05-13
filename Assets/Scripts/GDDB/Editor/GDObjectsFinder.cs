using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
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

        public Boolean IsDuplicatedType( GDObject gdObject )
        {
            return IsDuplicatedType( gdObject.Type );
        }

        public Boolean IsDuplicatedType( GdType type )
        {
            if ( type == default )
                return false;

            var typeCount = 0;
            for ( var i = 0; i < GDTypedObjects.Count; i++ )
            {
                if ( GDTypedObjects[ i ].Type == type )
                {
                    if ( typeCount > 0 )
                        return true;
                    else
                        typeCount++;
                }                
            }

            return false;
        }


        public Boolean FindFreeType( GdType fromType, out GdType result )
        {
            var from  = fromType[3];
            var range = Math.Max( 255 - from, from );
            for ( var i = 1; i < range; i++ )
            {
                var incValue = from + i;
                if ( incValue <= 255 )
                {
                    if ( CheckValue( incValue ) )
                    {
                        result = fromType.WithCategory( 3, incValue );
                        return true;
                    }
                }

                var decValue = from - i;
                if ( decValue >= 0 )
                {
                    if ( CheckValue( decValue ) )
                    {
                        result = fromType.WithCategory( 3, decValue );
                        return true;
                    }
                }
            }

            result = default;
            return false;

            Boolean CheckValue( Int32 newElem )
            {
                var newType = fromType.WithCategory( 3, newElem );
                return !GDTypedObjects.Exists( g => g.Type == newType );
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