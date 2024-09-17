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
    public class GDOAssetPostprocessor : AssetPostprocessor
    {
        private static readonly List<GDObject> _gdObjects = new ();
        private static readonly List<String>   _folders   = new ();
        private static Boolean _isNeedRecompileGddb;

        static GDOAssetPostprocessor()
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

            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;

            void EditorApplicationOnplayModeStateChanged(PlayModeStateChange state )
            {
                if ( _isNeedRecompileGddb && state  == PlayModeStateChange.ExitingEditMode )
                {
                    _isNeedRecompileGddb = false;
                    AssetDatabase.ImportAsset( "Assets/Scripts/GDDB/AssemblyInfo.cs", ImportAssetOptions.ForceUpdate );
                }
            }
        }

        private static void OnPostprocessAllAssets(String[] importedAssets, String[] deletedAssets, String[] movedAssets, String[] movedFromAssetPaths, bool isDomainReload )
        {
            if ( isDomainReload )
            {
                // var loader = new GdEditorLoader( );
                // var gdb         = loader.GetGameDataBase();
                // var properties = gdb.Root.Mobs.GetType().GetProperties();
                // foreach ( var propertyInfo in properties )
                // {
                //     Debug.Log( propertyInfo.Name );
                // }
            }

            _gdObjects.Clear();
            _folders.Clear();
        
            for ( var i = 0; i < importedAssets.Length; i++ )
            {
                var assetPath = importedAssets[ i ];
                if ( assetPath.EndsWith( ".asset" ) )
                {
                    var gdObject  = AssetDatabase.LoadAssetAtPath<GDObject>( assetPath );
                    if ( gdObject )
                    {
                        _gdObjects.Add( gdObject );
                    }
                }
                else if ( AssetDatabase.IsValidFolder( assetPath ) )        //Check for gddb folder structure change
                {
                    _folders.Add( assetPath );
                }
            }

            //Check for gddb folder structure change
            for ( int i = 0; i < deletedAssets.Length; i++ )
            {
                var assetPath = deletedAssets[ i ];
                if ( !Path.HasExtension( assetPath ) )        
                {
                    _folders.Add( assetPath );
                }
            }
            
            if( _gdObjects.Count > 0 )
            {
                //ProcessImportedGDObjects( _gdObjects );
            }
            if( _folders.Count > 0 )
            {
                ProcessImportedFolders( _folders );
            }
        }

        private static void ProcessImportedFolders( List<String> folders )
        {
            //Check if changed folders are in GDDB structure
            var isGDFoldersChanged = false;
            var foldersParser = new FoldersParser();
            foldersParser.Parse();

            foreach ( var folder in folders )
            {
                if( folder.StartsWith( foldersParser.RootFolderPath ) )
                {
                    isGDFoldersChanged = true;
                    break;
                }
            }

            if ( isGDFoldersChanged )
            {
                Debug.Log( $"[{nameof(GDOAssetPostprocessor)}] GD folder structure change detected, update GDDB DOM" );

                //Update gd structure json to trigger GDDB DOM source generator
                var       foldersSerializer = new FoldersSerializer();
                var       jsonString        = foldersSerializer.Serialize( foldersParser.Root );
                var       path              = $"{Application.dataPath}/Scripts/TreeStructure.json";
                using var treeStructureFile = File.CreateText( path );
                treeStructureFile.Write( jsonString );
                treeStructureFile.Close();
                Debug.Log( $"[{nameof(GDOAssetPostprocessor)}] Updated structure: {jsonString}" );


                var currentWindow = EditorWindow.focusedWindow;
                if( currentWindow )
                    currentWindow.ShowNotification( new GUIContent( "GDDB folders changed, need to recompile scripts" ) );
                _isNeedRecompileGddb = true;

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
        
    }
}