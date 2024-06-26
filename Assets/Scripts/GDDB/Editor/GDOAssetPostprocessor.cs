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

            var gdoFinder = new GDObjectsFinder();
            foreach ( var gdObject in gdObjects )
            {
                if( gdObject.Type != default )
                    if ( gdoFinder.IsDuplicatedType( gdObject ) )
                    {
                        if( gdoFinder.FindFreeType( gdObject.Type, out var newType ) )
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
                    }
            }

            Debug.Log( $"Finished processing imported GDObjects" );
        }
    }
}