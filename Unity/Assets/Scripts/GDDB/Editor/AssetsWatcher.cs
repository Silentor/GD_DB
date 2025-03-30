using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    /// <summary>
    /// Watch for changes of GDDB structure in Asset Database 
    /// </summary>
    public class AssetsWatcher : AssetPostprocessor
    {
        public static event Action GddbAssetsChanged;

        private static void OnPostprocessAllAssets(String[] importedAssets, String[] deletedAssets, String[] movedAssets, String[] movedFromAssetPaths, bool isDomainReload )
        {
            //DEBUG
            // if ( isDomainReload )
            // {
            //     var gdb           = GDBEditor.GDB;
            //     var rootGenerated = typeof(GdDb).GetProperty( "Root" );
            //     if ( rootGenerated != null )
            //     {
            //         var rootObj        = rootGenerated.GetValue( gdb );
            //         var generatedProps = rootObj.GetType().GetProperties();
            //         foreach ( var propertyInfo in generatedProps )
            //         {
            //             Debug.Log( propertyInfo.Name );
            //         }
            //     }
            // }
        
            //If Gddb is not present, we just try to find GDRoot
            if ( EditorDB.DB == null )
            {
                for ( var i = 0; i < importedAssets.Length; i++ )
                {
                    var assetPath = importedAssets[ i ];
                    if ( assetPath.EndsWith( ".asset", StringComparison.Ordinal ) )
                    {
                        var gdroot  = AssetDatabase.LoadAssetAtPath<GDRoot>( assetPath );
                        if ( gdroot )
                        {
                            //Any changes in gdroot should be processed
                            GddbAssetsChanged?.Invoke(  );
                            return;
                        }
                    }
                }
            }
            else    //If Gddb present, check any ScriptableObjects changes in gddb folder
            {
                var dbRootPath = EditorDB.RootFolderPath;
                for ( var i = 0; i < importedAssets.Length; i++ )
                {
                    var assetPath = importedAssets[ i ];
                    if ( assetPath.StartsWith(dbRootPath, StringComparison.Ordinal) && assetPath.EndsWith( ".asset", StringComparison.Ordinal )
                        && AssetDatabase.LoadAssetAtPath<ScriptableObject>( assetPath )) 
                    {
                        GddbAssetsChanged?.Invoke(  );
                        return;
                    }
                }

                for ( var i = 0; i < deletedAssets.Length; i++ )
                {
                    var assetPath = deletedAssets[ i ];
                    if ( assetPath.StartsWith(dbRootPath, StringComparison.Ordinal) && assetPath.EndsWith( ".asset", StringComparison.Ordinal )) 
                    {
                        GddbAssetsChanged?.Invoke(  );
                        return;
                    }
                }

                for ( var i = 0; i < movedAssets.Length; i++ )
                {
                    var assetPath = movedAssets[ i ];
                    if ( assetPath.StartsWith(dbRootPath, StringComparison.Ordinal) && assetPath.EndsWith( ".asset", StringComparison.Ordinal )
                        && AssetDatabase.LoadAssetAtPath<ScriptableObject>( assetPath )) 
                    {
                        GddbAssetsChanged?.Invoke(  );
                        return;
                    }
                }

                for ( var i = 0; i < movedFromAssetPaths.Length; i++ )
                {
                    var assetPath = movedFromAssetPaths[ i ];
                    if ( assetPath.StartsWith(dbRootPath, StringComparison.Ordinal) && assetPath.EndsWith( ".asset", StringComparison.Ordinal ) ) 
                    {
                        GddbAssetsChanged?.Invoke(  );
                        return;
                    }
                }

            }
        }

        // private class GDAssetModificationProcessor : AssetModificationProcessor
        // {
        //     private static AssetDeleteResult OnWillDeleteAsset(String assetPath, RemoveAssetOptions options )
        //     {
        //         if ( assetPath.EndsWith( ".asset" ) )
        //         {
        //             var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
        //             if ( gdObject )
        //             {
        //                 _removedGDObjects.Add( assetPath );
        //             }
        //         }
        //
        //         return AssetDeleteResult.DidNotDelete;
        //     }
        // }
        
    }

    
}