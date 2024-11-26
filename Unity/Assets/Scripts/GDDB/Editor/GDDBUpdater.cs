using System;
using System.IO;
using System.Linq;
using GDDB.Serialization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GDDB.Editor
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
            GDAssets.GDDBAssetsChanged.Subscribe( (_, _) => _isDBDirty = true );
            GDObjectEditor.Changed += _ => _isDBDirty = true;
        }

        private static void EditorApplicationOnplayModeStateChanged( PlayModeStateChange state )
        {
            if ( state == PlayModeStateChange.ExitingEditMode )
            {
                var settings = UpdateDBSettings.instance;
                if ( settings.AutoUpdateOnRun )
                {
                    AssetDatabase.SaveAssets();
                    var editorDB = GDBEditor.GDB;
                    UpdateScriptableObjectDB( settings, editorDB );
                    UpdateJsonDB( settings, editorDB );
                    _isDBDirty = false;
                }
            }
        }

        public void  OnPreprocessBuild( BuildReport buildReport )
        {
            BuildPreprocessing?.Invoke();

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

            if ( settings.AutoUpdateOnBuild )
            {
                AssetDatabase.SaveAssets();
                var editorDB   = GDBEditor.GDB;

                if ( editorDB.AllObjects.Any() )
                {
                    _isDBDirty = true;
                    try
                    {
                        UpdateScriptableObjectDB( settings, editorDB );
                        UpdateJsonDB( settings, editorDB );
                    }
                    catch ( Exception e )
                    {
                        Debug.LogError( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] Exception {e} while saving GDDB. Build aborted." );
                        throw new BuildFailedException( e );
                    }
                    _isDBDirty = false;
                }
                else
                {
                    Debug.LogWarning( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] No GDDB detected, there is nothing to update" );
                }

            }
        }

        private static void UpdateJsonDB(UpdateDBSettings settings, GdDb editorDB )
        {
            if ( settings.UpdateJsonDB && !String.IsNullOrEmpty( settings.JsonDBPath ) 
                && (_isDBDirty || !File.Exists( settings.JsonDBPath ) || (!string.IsNullOrEmpty(settings.JsonAssetsReferencePath) && !File.Exists( settings.JsonAssetsReferencePath ))) )
            {
                var serializer      = new DBJsonSerializer();
                var rootFolder      = editorDB.RootFolder;
                var assetReferencer = ScriptableObject.CreateInstance<DirectAssetReferences>();
                var json            = serializer.Serialize( rootFolder, editorDB.AllObjects, assetReferencer );
                File.WriteAllText( settings.JsonDBPath, json );
                var jsonLog = $"Saved GDDB to json format to {settings.JsonDBPath}";
                if ( !String.IsNullOrEmpty( settings.JsonAssetsReferencePath ) )
                {
                    AssetDatabase.CreateAsset( assetReferencer, settings.JsonAssetsReferencePath );
                    AssetDatabase.Refresh( ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );
                    jsonLog += $", and assets reference to {settings.JsonAssetsReferencePath}";
                }
                    
                Debug.Log( $"[{nameof(GDDBUpdater)}]-[{nameof(OnPreprocessBuild)}] " + jsonLog );
            }
        }

        private static void UpdateScriptableObjectDB(UpdateDBSettings settings, GdDb editorDB )
        {
            if ( settings.UpdateScriptableObjectDB && !String.IsNullOrEmpty( settings.ScriptableObjectDBPath )
                && (_isDBDirty || !AssetDatabase.LoadAssetAtPath<DBAsset>( settings.ScriptableObjectDBPath ) ) )
            {
                var serializer = new DBScriptableObjectSerializer();
                var rootFolder = editorDB.RootFolder;
                var asset      = serializer.Serialize( rootFolder );
                var path       = settings.ScriptableObjectDBPath;

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
