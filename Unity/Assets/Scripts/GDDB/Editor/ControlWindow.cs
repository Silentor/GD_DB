using System;
using System.Collections.Generic;
using System.IO;
using GDDB.Serialization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace GDDB.Editor
{
    public class ControlWindow : EditorWindow
    {
        private Label         _dbStatsLbl;
        private Label         _dbHashLbl;
        private Label         _dbValidationLbl;
        private Label         _sourceGenFileHashLbl;
        private VisualElement _sourceGenFileHashIcon;
        private Button        _sourceGenBtn;
        private Label         _sourceGenCodeHashLbl;
        private VisualElement _sourceGenCodeHashIcon;
        private VisualElement _gettingStartedBlock;


        private void Awake( )
        {
            //Debug.Log( $"[{nameof(ControlWindow)}]-[{nameof(OnEnable)}] " );
            Validator.Validated += OnValidated;
            GDBSourceGenerator.SourceUpdated += UpdateSourceGenInfo;
            GDBEditor.Updated += OnEditorDBUpdated;
        }
        
        private void OnDestroy( )
        {
            Validator.Validated              -= OnValidated;
            GDBSourceGenerator.SourceUpdated -= UpdateSourceGenInfo;
            GDBEditor.Updated                -= OnEditorDBUpdated;

            SourceGeneratorSettings.instance.Save();
            UpdateDBSettings.instance.Save();
        }

        private void CreateGUI( )
        {
            var window = UnityEngine.Resources.Load<VisualTreeAsset>( "ControlWindow" ).Instantiate();

            _dbStatsLbl = window.Q<Label>( "GDBInfo" );
            _dbHashLbl = window.Q<Label>( "GDBHash" );
            _dbValidationLbl = window.Q<Label>( "Validation" );

            //Show getting started section if no GDDB detected
            _gettingStartedBlock = window.Q<VisualElement>( "GettingStarted" );
            _gettingStartedBlock.Q<Button>( "SelectRootFolderBtn" ).clicked += CreateNewGDRootAsset;
            _gettingStartedBlock.style.display = GDBEditor.DB == null ? DisplayStyle.Flex : DisplayStyle.None;

            //Source Gen settings
            var sourceGenSettingsBox = window.Q<VisualElement>( "SourceGenSettings" );
            var container     = sourceGenSettingsBox.Q<VisualElement>( "SerializedSettings" );
            var sgSettings      = SourceGeneratorSettings.instance;
            container.Add( new InspectorElement(sgSettings) );
            _sourceGenFileHashLbl =  sourceGenSettingsBox.Q<Label>( "SourceGenFileHash" );
            _sourceGenFileHashIcon         =  sourceGenSettingsBox.Q<VisualElement>( "SourceGenFileHashIcon" );
            _sourceGenCodeHashLbl =  sourceGenSettingsBox.Q<Label>( "SourceGenCodeHash" );
            _sourceGenCodeHashIcon         =  sourceGenSettingsBox.Q<VisualElement>( "SourceGenCodeHashIcon" );
            _sourceGenBtn              =  sourceGenSettingsBox.Q<Button>( "SourceGenBtn" );
            _sourceGenBtn.clicked      += GenerateSourceManual;

            //Update DB settings
            var updateDBSettingsBox = window.Q<VisualElement>( "DBUpdateSettings" );
            container            = updateDBSettingsBox.Q<VisualElement>( "SerializedSettings" );
            var updateSettings             = UpdateDBSettings.instance;
            container.Add( new InspectorElement(updateSettings) );

            var soUpdateBtn = updateDBSettingsBox.Q<Button>( "SoDBUpdate" );
            soUpdateBtn.clicked += () => SaveGDDBToSO( updateSettings );
            var jsonUpdateBtn = updateDBSettingsBox.Q<Button>( "JsonDBUpdate" );
            jsonUpdateBtn.clicked += () => SaveGDDBToJson ( updateSettings );
            var binUpdateBtn = updateDBSettingsBox.Q<Button>( "BinDBUpdate" );
            binUpdateBtn.clicked += () => SaveGDDBToBinary ( updateSettings );

            UpdateGDBInfo();
            UpdateSourceGenInfo();
            OnValidated( Validator.Reports  );

            rootVisualElement.Add( window );
        }

        

        private void OnValidated( IReadOnlyList<ValidationReport> reports )
        {
            if( _dbValidationLbl == null )
                return;

            _dbValidationLbl.text = $"Validation: errors {reports.Count}";
        }

        private void GenerateSourceManual( )
        {
            GDBSourceGenerator.GenerateGDBSource( true );
        }

        private void SaveGDDBToSO( UpdateDBSettings updateSettings )
        {
            if ( String.IsNullOrEmpty( updateSettings.ScriptableObjectDBPath ) ) throw new InvalidOperationException( "Output path is empty" );

            GDDBUpdater.UpdateScriptableObjectDB( updateSettings, GDBEditor.DB );
            var result = AssetDatabase.LoadAssetAtPath<DBScriptableObject>( updateSettings.ScriptableObjectDBPath  );
            if( result )
                EditorGUIUtility.PingObject( result );

            if ( Validator.Reports.Count > 0 ) Debug.LogError( $"[{nameof(ControlWindow)}] GDDB validation errors detected, count {Validator.Reports.Count}" );
        }

        private void SaveGDDBToJson( UpdateDBSettings updateSettings )
        {
            if ( String.IsNullOrEmpty( updateSettings.JsonDBPath ) ) throw new InvalidOperationException( "Output path is empty" );

            GDDBUpdater.UpdateJsonDB( updateSettings, GDBEditor.DB, true );
            EditorUtility.RevealInFinder( updateSettings.JsonDBPath );

            if ( Validator.Reports.Count > 0 ) Debug.LogError( $"[{nameof(ControlWindow)}] GDDB validation errors detected, count {Validator.Reports.Count}" );
        }
        
        private void SaveGDDBToBinary( UpdateDBSettings updateSettings )
        {
            if ( String.IsNullOrEmpty( updateSettings.BinaryDBPath ) ) throw new InvalidOperationException( "Output path is empty" );

            GDDBUpdater.UpdateBinaryDB( updateSettings, GDBEditor.DB, true );
            EditorUtility.RevealInFinder( updateSettings.BinaryDBPath );

            if ( Validator.Reports.Count > 0 ) Debug.LogError( $"[{nameof(ControlWindow)}] GDDB validation errors detected, count {Validator.Reports.Count}" );
        }

        private void OnEditorDBUpdated( )
        {
            _gettingStartedBlock.style.display = GDBEditor.DB == null ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateGDBInfo( );
            UpdateSourceGenInfo();
        }

        private void UpdateGDBInfo( )
        {
            //Can be called before GUI is created from event handler
            if( _dbStatsLbl == null || GDBEditor.DB == null )
                return;

            var rootFolder   = GDBEditor.DB.RootFolder;
            var rootFullPath = AssetDatabase.GUIDToAssetPath( rootFolder.FolderGuid.ToString("N") );
            _dbStatsLbl.text = $"GDB root folder: {rootFullPath}, objects {GDBEditor.AllObjects.Count}, folders {GDBEditor.AllFolders.Count}";
            _dbHashLbl.text  = "DB structure hash: " + rootFolder.GetFoldersChecksum( );
        }

        private void UpdateSourceGenInfo( )
        {
            //Can be called before GUI is created somehow
            if( _sourceGenFileHashLbl == null || GDBEditor.DB == null )
                return;

            var rootFolderHash        = GDBEditor.DB.RootFolder.GetFoldersChecksum();
            var generatedFileHash = GDBSourceGenerator.GetGeneratedFileChecksum();
            var generatedCodeHash = GDBSourceGenerator.GetGeneratedCodeChecksum();
            _sourceGenFileHashLbl.text = "Generated file hash: " + generatedFileHash;
            _sourceGenFileHashIcon.style.backgroundImage = rootFolderHash == generatedFileHash ? Resources.HashOkIcon : Resources.HashNotOkIcon;
            _sourceGenCodeHashLbl.text = "Generated code hash: " + generatedCodeHash;
            _sourceGenCodeHashIcon.style.backgroundImage = rootFolderHash == generatedCodeHash ? Resources.HashOkIcon : Resources.HashNotOkIcon;
        }

        private static void CreateNewGDRootAsset( )
        {
            var newRootFolder = EditorUtility.OpenFolderPanel( "Select GDDB root folder", Application.dataPath, "" );
            if ( !String.IsNullOrEmpty( newRootFolder ) )
            {
                if ( newRootFolder == Application.dataPath )
                {
                    EditorUtility.DisplayDialog( "Error",
                            "Please do not select Assets folder as root folder of game design data base. You do not want to include to GDDB all assets of your project. Select some dedicated subfolder for game design data.",
                            "Cancel" );
                    return;
                }

                if( !newRootFolder.StartsWith( Application.dataPath ) )
                {
                    EditorUtility.DisplayDialog( "Error",
                            "Please select root folder somewhere inside your project Assets folder.",
                            "Cancel" );
                    return;
                }

                if ( !System.IO.Directory.Exists( newRootFolder ) )
                {
                    Directory.CreateDirectory( newRootFolder );
                    AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
                }

                var newGddbRoot = GDObject.CreateInstance<GDRoot>();
                newGddbRoot.name = "GDRoot";
                newRootFolder = newRootFolder.Replace( Application.dataPath, "Assets" );
                AssetDatabase.CreateAsset( newGddbRoot, $"{newRootFolder}/{newGddbRoot.name}.asset" );
                EditorGUIUtility.PingObject( newGddbRoot );
            }
        }

        private static class Resources
        {
            public static readonly Texture2D HashOkIcon    = UnityEngine.Resources.Load<Texture2D>( "check_24dp" );
            public static readonly Texture2D HashNotOkIcon = UnityEngine.Resources.Load<Texture2D>( "error_24dp" );
        }
    }
}