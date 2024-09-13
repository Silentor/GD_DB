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

            AssetDatabase.StartAssetEditing();

            try
            {
                //Make sure all GDObjects have GUIDs
                foreach ( var gdObject in gdObjects )
                {
                    if( gdObject.Guid == default )
                    {
                        if ( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( gdObject, out var guid, out long _ ) )
                        {
                            var newGuid = Guid.ParseExact( guid, "N" ); 
                            gdObject.SetGuid( newGuid ); 
                        }
                        else
                        {
                            Debug.LogError( $"[{nameof(GDOAssetPostprocessor)} Error getting guid for GDObject {gdObject.Name}]" );
                        }
                    }
                    else if(  AssetDatabase.TryGetGUIDAndLocalFileIdentifier( gdObject, out var assetGuid, out long _ ) && gdObject.Guid.ToString("N") != assetGuid )
                    {
                        //Probably duplicated object
                        var guid = Guid.ParseExact( assetGuid, "N" );
                        gdObject.SetGuid( guid );
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            
        
            Debug.Log( "Finished processing imported GDObjects" );
        }
        
    }
}