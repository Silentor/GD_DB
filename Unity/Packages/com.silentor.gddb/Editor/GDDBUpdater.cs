using System;
using System.IO;
using System.Linq;
using System.Text;
using Gddb.Editor.Validations;
using Gddb.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using BinaryWriter = Gddb.Serialization.BinaryWriter;
using Serialization_BinaryWriter = Gddb.Serialization.BinaryWriter;

namespace Gddb.Editor
{
    /// <summary>
    /// Update GDDB files before build of play mode enter
    /// </summary>
    [InitializeOnLoad]
    public class GDDBUpdater : IPreprocessBuildWithReport
    {
        public Int32 callbackOrder { get; } = 0;

        private static Boolean _isDBDirty = true;      //To prevent excessive updates on play mode enter

        public static event Action BuildPreprocessing;

        static GDDBUpdater( )
        {
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            EditorDB.Updated += ( ) => _isDBDirty = true;
            GDObjectEditor.Changed += _ => _isDBDirty = true;
        }

        private static void EditorApplicationOnplayModeStateChanged( PlayModeStateChange state )
        {
            if ( state == PlayModeStateChange.ExitingEditMode && EditorDB.DB != null )
            {
                var settings = UpdateDBSettings.instance;

                if ( settings.ScriptableDBSettings.UpdateOnEditorRun )
                {
                    AssetDatabase.SaveAssets();
                    UpdateScriptableObjectDB( settings.ScriptableDBSettings, EditorDB.DB );
                }

                if ( settings.JsonDBSettings.UpdateOnEditorRun )
                {
                    AssetDatabase.SaveAssets();
                    UpdateJsonDB( settings.JsonDBSettings, settings.AssetsReferencePath, EditorDB.DB );
                }

                if ( settings.BinDBSettings.UpdateOnEditorRun )
                {
                    AssetDatabase.SaveAssets();
                    UpdateBinaryDB( settings.BinDBSettings, settings.AssetsReferencePath, EditorDB.DB );
                }

                _isDBDirty = false;
            }
        }

        public void  OnPreprocessBuild( BuildReport buildReport )
        {
            BuildPreprocessing?.Invoke();

            //Nothing to do if no GDDB present
            if( EditorDB.DB == null )
                return;

            var settings   = UpdateDBSettings.instance;
            
            //Validate DB before build and abort build if errors found
            if ( settings.ValidateDBOnBuild )
            {
                AssetDatabase.SaveAssets();
                var reports = Validator.Validate( );
                if( reports.Count > 0 )
                {
                    Debug.LogError( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] Build stopped because there are GDDB validation errors, count {reports.Count}:" );
                    foreach( var report in reports )                        
                        Debug.LogError( report, report.GdObject );
                    throw new BuildFailedException( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] Build stopped because there are GDDB validation errors, count {reports.Count}.");
                }
            }

            try
            {
                if ( settings.ScriptableDBSettings.UpdateOnEditorRun )
                {
                    AssetDatabase.SaveAssets();
                    UpdateScriptableObjectDB( settings.ScriptableDBSettings, EditorDB.DB, true );
                }

                if ( settings.JsonDBSettings.UpdateOnEditorRun )
                {
                    AssetDatabase.SaveAssets();
                    UpdateJsonDB( settings.JsonDBSettings, settings.AssetsReferencePath, EditorDB.DB, true );
                }

                if ( settings.BinDBSettings.UpdateOnEditorRun )
                {
                    AssetDatabase.SaveAssets();
                    UpdateBinaryDB( settings.BinDBSettings, settings.AssetsReferencePath, EditorDB.DB, true );
                }
            }
            catch( Exception ex )
            {
                Debug.LogError( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] Exception {ex.GetType().Name} while saving Gddb. Build aborted: {ex}" );
                throw new BuildFailedException( ex );
            }
        }

        public static Boolean UpdateJsonDB(UpdateDBSettings.JsonDbSettings settings, string assetsReferencePath, GdDb editorDB, Boolean force = false )
        {
            Assert.IsTrue( !String.IsNullOrEmpty( settings.Path ), "JSON DB output path is not defined" );

            var isDBInvalid = force || _isDBDirty || !File.Exists( settings.Path );
            var isAssetsReferenceInvalid = force || (!string.IsNullOrEmpty(assetsReferencePath) && !File.Exists( assetsReferencePath ));
            if ( isDBInvalid || isAssetsReferenceInvalid ) 
            {
                var serializer      = new DBDataSerializer();
                var rootFolder      = editorDB.RootFolder;
                var assetReferencer = ScriptableObject.CreateInstance<DirectAssetReferences>();
                var buffer          = new StringBuilder();
                var indent          = !BuildPipeline.isBuildingPlayer;
                var writer          = new JsonNetWriter( buffer, indent );
                serializer.Serialize( writer, rootFolder, assetReferencer );
                File.WriteAllText( settings.Path, buffer.ToString() );
                var jsonLog = $"Saved GDDB to json format to {settings.Path}";
                if ( !String.IsNullOrEmpty( assetsReferencePath ) )
                {
                    AssetDatabase.CreateAsset( assetReferencer, assetsReferencePath );
                    AssetDatabase.Refresh( ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
                    jsonLog += $", and assets reference to {assetsReferencePath}";
                }
                    
                Debug.Log( $"[{nameof(GDDBUpdater)}]-[{nameof(UpdateJsonDB)}] " + jsonLog );

                return true;
            }

            return false;
        }
        
        public static Boolean UpdateBinaryDB( UpdateDBSettings.DbSettings settings, string assetsReferencePath, GdDb editorDB, Boolean force = false )
        {
            Assert.IsTrue( !String.IsNullOrEmpty( settings.Path ), "Binary DB output path is not defined" );

            var isDBInvalid = force || _isDBDirty || !File.Exists( settings.Path );
            var isAssetsReferenceInvalid = force || (!string.IsNullOrEmpty(assetsReferencePath) && !File.Exists( assetsReferencePath ));
            if ( isDBInvalid || isAssetsReferenceInvalid ) 
            {
                var serializer      = new DBDataSerializer();
                var rootFolder      = editorDB.RootFolder;
                var assetReferencer = ScriptableObject.CreateInstance<DirectAssetReferences>();
                var buffer          = new MemoryStream();
                var writer          = new Serialization_BinaryWriter( buffer );
                serializer.Serialize( writer, rootFolder, assetReferencer );
                File.WriteAllBytes( settings.Path, buffer.ToArray() );
                var log = $"Saved GDDB to binary format to {settings.Path}, file size {buffer.Length} bytes";
                if ( !String.IsNullOrEmpty( assetsReferencePath ) )
                {
                    AssetDatabase.CreateAsset( assetReferencer, assetsReferencePath );
                    AssetDatabase.Refresh( ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
                    log += $", and assets reference to {assetsReferencePath}";
                }
                    
                Debug.Log( $"[{nameof(GDDBUpdater)}]-[{nameof(UpdateBinaryDB)}] " + log );

                return true;
            }

            return false;
        }

        public static void UpdateScriptableObjectDB(UpdateDBSettings.DbSettings settings, GdDb editorDB, Boolean force = false )
        {
            Assert.IsTrue( !String.IsNullOrEmpty( settings.Path ), "Scriptable Object DB output path is not defined" );

            var isDBInvalid = force || _isDBDirty || !File.Exists( settings.Path );
            if ( isDBInvalid ) 
            {
                var serializer = new DBScriptableObjectSerializer();
                var rootFolder = editorDB.RootFolder;
                var asset      = serializer.Serialize( rootFolder );
                var path       = settings.Path;

                try
                {
                    AssetDatabase.CreateAsset( asset, path);
                    AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );  //Workaround for db asset not being loaded for subsequent runs
                }
                catch ( UnityException uex )
                {
                    Debug.LogError( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] Failed to save GDDB to Scriptable Object format {path}. Probably incorrect path or filename?" );
                    throw;
                }

                Debug.Log( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] Saved GDDB to Scriptable Object format {path}", asset );
            }
        }
    }
}
