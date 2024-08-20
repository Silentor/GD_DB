using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    public class GDOAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(String[] importedAssets, String[] deletedAssets, String[] movedAssets, String[] movedFromAssetPaths )
        {
            var gdObjects = new List<GDObject>();

            for ( var i = 0; i < importedAssets.Length; i++ )
            {
                var assetPath = importedAssets[ i ];
                if ( assetPath.EndsWith( ".asset" ) )
                {
                    var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
                    if ( gdObject )
                    {
                        gdObjects.Add( gdObject );
                    }
                }
            }
            
            if( gdObjects.Count > 0 )
            {
                ProcessImportedGDObjects( gdObjects );
            }
        }

        private static void ProcessImportedGDObjects( List<GDObject> gdObjects )
        {
            Debug.Log( $"Processing imported GDObjects, count {gdObjects.Count}" );

            GDTypeHierarchy gdTypeHierarchy = null;
            var gdoFinder = new GDObjectsFinder();
            foreach ( var gdObject in gdObjects )
            {
                if( gdObject.Type != default )
                {
                    if(_gdTypeHierarchy == null)
                    {
                        _gdTypeHierarchy = new GDTypeHierarchy();
                    }

                    var metadata = _gdTypeHierarchy.GetMetadataOf( gdObject.Type );
                    if ( gdoFinder.IsDuplicatedType( gdObject.Type, metadata, out _ ) )
                    {
                        if( gdoFinder.FindFreeType( gdObject.Type, metadata, out var newType ) )
                        {
                            var oldType = gdObject.Type;
                            gdObject.Type = newType;
                            EditorUtility.SetDirty( gdObject );
                            AssetDatabase.SaveAssetIfDirty( gdObject );
                            Debug.LogWarning( $"[GDOAssetPostprocessor] Detected duplicate type on imported GDObject, type autoincremented: object {gdObject.name}, old type {oldType}, new type {newType}", gdObject );
                        }
                        else
                        {
                            Debug.LogWarning( $"[GDOAssetPostprocessor] Detected duplicate type on imported GDObject, but no free type was found: object {gdObject.name}, type {gdObject.Type}", gdObject );
                        }
                    }}
            }

            Debug.Log( $"Finished processing imported GDObjects" );
        }

        private static GDTypeHierarchy _gdTypeHierarchy;
    }
}