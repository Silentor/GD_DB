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
            if ( gdObject.Type == default )
                return false;

            return GDTypedObjects.Exists( g => g.Type == gdObject.Type && g != gdObject );
        }

        public Boolean FindFreeType( GdType fromType, out GdType result )
        {
            for ( var i = fromType[3]; i <= 255; i++ )
            {
                var newType = fromType.WithCategory( 3, i );
                if ( !GDTypedObjects.Exists( g => g.Type == newType ) )
                {
                    result = newType;
                    return true;
                }
            }

            result = default;
            return false;
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