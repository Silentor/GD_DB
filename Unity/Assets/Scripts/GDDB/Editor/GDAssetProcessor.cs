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
    public class GDAssetProcessor : UnityEditor.AssetPostprocessor
    {
        /// <summary>
        /// Lists are reused
        /// </summary>
        public static readonly PriorityEvent<IReadOnlyList<GDObject>, IReadOnlyList<String>> GDDBAssetsChanged = new ();

        private static readonly List<GDObject>  _modifiedGDObjects = new ();
        private static readonly List<String>    _removedGDObjects = new ();

        static GDAssetProcessor()
        {
            CompilationPipeline.compilationStarted += CompilationPipelineOncompilationStarted;

            void CompilationPipelineOncompilationStarted(Object obj )
            {
                Debug.Log( $"started compilation of assembly {obj} ({obj.GetHashCode()})" );
            }

            CompilationPipeline.compilationFinished += CompilationPipelineOncompilationFinished;

            void CompilationPipelineOncompilationFinished(Object obj )
            {
                Debug.Log( $"finished compilation of assembly {obj} ({obj.GetHashCode()})" );
            }

            CompilationPipeline.assemblyCompilationFinished += CompilationPipelineOnassemblyCompilationFinished;

            void CompilationPipelineOnassemblyCompilationFinished(String arg1, CompilerMessage[] arg2 )
            {
                Debug.Log( $"finished compilation to {arg1}, messages {String.Join( ", ", arg2.Select( cm => cm.message ) )}" );
            }

            CompilationPipeline.assemblyCompilationNotRequired += CompilationPipelineOnassemblyCompilationNotRequired;

            void CompilationPipelineOnassemblyCompilationNotRequired(String obj )
            {
                Debug.Log( $"not required compilation of assembly {obj} ({obj.GetHashCode()})" );
            }
        }

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
        
            for ( var i = 0; i < importedAssets.Length; i++ )
            {
                var assetPath = importedAssets[ i ];
                if ( assetPath.EndsWith( ".asset" ) )
                {
                    var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
                    if ( gdObject )
                    {
                        _modifiedGDObjects.Add( gdObject );
                    }
                }
            }


            // for ( int i = 0; i < deletedAssets.Length; i++ )
            // {
            //     Debug.Log( $"[{nameof(GDAssets)}]-[{nameof(OnPostprocessAllAssets)}] removed {deletedAssets[0]}" );
            //
            //     //Check if deleted assets was from GD DB (by path)
            //     var assetPath = deletedAssets[ i ];
            //     if ( assetPath.EndsWith( ".asset" ) )
            //     {
            //         var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
            //         if ( gdObject )
            //         {
            //             _modifiedObjects.Add( gdObject );
            //         }
            //     }
            // }

            for ( int i = 0; i < movedAssets.Length; i++ )
            {
                var assetPath = movedAssets[ i ];
                if ( assetPath.EndsWith( ".asset" ) )
                {
                    var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
                    if ( gdObject )
                    {
                        _modifiedGDObjects.Add( gdObject );
                    }
                }
            }
            
            if( _modifiedGDObjects.Count > 0 || _removedGDObjects.Count > 0 )
            {
                Debug.Log( $"[{nameof(GDAssetProcessor)}]-[{nameof(OnPostprocessAllAssets)}] changed {_modifiedGDObjects.Count}, removed {_removedGDObjects.Count} GDObjects" );
                GDDBAssetsChanged?.Invoke( _modifiedGDObjects, _removedGDObjects );
            }

            _modifiedGDObjects.Clear();
            _removedGDObjects.Clear();
        }

        private static void ProcessImportedFolders( List<String> folders )
        {
            //if ( isGDFoldersChanged )
            {
                // var currentWindow = EditorWindow.focusedWindow;
                // if( currentWindow )
                //     currentWindow.ShowNotification( new GUIContent( "GDDB folders changed, need to recompile scripts" ) );
                // _isNeedRecompileGddb = true;

                //AssetDatabase.ImportAsset( "Assets/Scripts/GDDB/AssemblyInfo.cs", ImportAssetOptions.ForceUpdate );
            }
        }

        private static void ProcessImportedGDObjects( List<GDObject> gdObjects )
        {
            // Debug.Log( $"Processing imported GDObjects, count {gdObjects.Count}" );
            //
            // AssetDatabase.StartAssetEditing();
            //
            // try
            // {
            //     //Make sure all GDObjects have GUIDs
            //     foreach ( var gdObject in gdObjects )
            //     {
            //         if( gdObject.Guid == default )
            //         {
            //             if ( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( gdObject, out var guid, out long _ ) )
            //             {
            //                 var newGuid = Guid.ParseExact( guid, "N" ); 
            //                 gdObject.SetGuid( newGuid ); 
            //             }
            //             else
            //             {
            //                 Debug.LogError( $"[{nameof(GDOAssetPostprocessor)} Error getting guid for GDObject {gdObject.Name}]" );
            //             }
            //         }
            //         else if(  AssetDatabase.TryGetGUIDAndLocalFileIdentifier( gdObject, out var assetGuid, out long _ ) && gdObject.Guid.ToString("N") != assetGuid )
            //         {
            //             //Probably duplicated object
            //             var guid = Guid.ParseExact( assetGuid, "N" );
            //             gdObject.SetGuid( guid );
            //         }
            //     }
            // }
            // finally
            // {
            //     AssetDatabase.StopAssetEditing();
            // }
            //
            //
            // Debug.Log( "Finished processing imported GDObjects" );
        }

        private class GDAssetModificationProcessor : AssetModificationProcessor
        {
            private static AssetDeleteResult OnWillDeleteAsset(String assetPath, RemoveAssetOptions options )
            {
                if ( assetPath.EndsWith( ".asset" ) )
                {
                    var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
                    if ( gdObject )
                    {
                        _removedGDObjects.Add( assetPath );
                    }
                }

                return AssetDeleteResult.DidNotDelete;
            }
        }
        
    }

    
}