﻿using System;
using System.Collections.Generic;
using System.IO;
using GDDB.Serialization;
using Newtonsoft.Json.Linq;
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
        private Label         _generatedStructureHashLbl;
        private VisualElement _sourceGenHashIcon;
        private Button        _sourceGenBtn;

        [MenuItem( "GDDB/Control Window" )]
        private static void ShowWindow( )
        {
            var window = GetWindow<ControlWindow>();
            window.titleContent = new GUIContent( "GDDB Control Window" );
            window.Show();
        }

        private void OnEnable( )
        {
            //Debug.Log( $"[{nameof(ControlWindow)}]-[{nameof(OnEnable)}] " );
            GDAssets.GDDBAssetsChanged.Subscribe( 10, OnGddbStructureChanged );
            Validator.Validated += OnValidated;
        }

        private void OnValidated( IReadOnlyList<ValidationReport> reports )
        {
            if( _dbValidationLbl == null )
                return;

            _dbValidationLbl.text = $"Validation: errors {reports.Count}";
        }

        private void OnGddbStructureChanged( IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> deletedObjects )
        {
            UpdateGDBInfo( );
            UpdateSourceGenInfo();
        }

        private void OnDisable( )
        {
            GDAssets.GDDBAssetsChanged.Unsubscribe( OnGddbStructureChanged );
            Validator.Validated -= OnValidated;
        }

        private void CreateGUI( )
        {
            Debug.Log( $"[{nameof(ControlWindow)}]-[{nameof(CreateGUI)}] " );
            var window = UnityEngine.Resources.Load<VisualTreeAsset>( "ControlWindow" ).Instantiate();

            _dbStatsLbl = window.Q<Label>( "GDBInfo" );
            _dbHashLbl = window.Q<Label>( "GDBHash" );
            _dbValidationLbl = window.Q<Label>( "Validation" );

            //Source Gen settings
            var sourceGenSettingsBox = window.Q<VisualElement>( "SourceGenSettings" );
            var container     = sourceGenSettingsBox.Q<VisualElement>( "SerializedSettings" );
            var sgSettings      = SourceGeneratorSettings.instance;
            container.Add( new InspectorElement(sgSettings) );
            _generatedStructureHashLbl =  sourceGenSettingsBox.Q<Label>( "SourceGenHash" );
            _sourceGenHashIcon         =  sourceGenSettingsBox.Q<VisualElement>( "SourceGenHashIcon" );
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

            UpdateGDBInfo();
            UpdateSourceGenInfo();
            OnValidated( Validator.Reports  );

            rootVisualElement.Add( window );
        }

        private void GenerateSourceManual( )
        {
            GDBSourceGenerator.GenerateGDBSource( true );
        }

        private void SaveGDDBToSO( UpdateDBSettings updateSettings )
        {
            if ( String.IsNullOrEmpty( updateSettings.ScriptableObjectDBPath ) ) throw new InvalidOperationException( "Output path is empty" );

            var editorDB   = GDBEditor.GDB;
            var serializer = new DBAssetSerializer();
            var soDB       = serializer.Serialize( editorDB.RootFolder );
            AssetDatabase.CreateAsset( soDB, updateSettings.ScriptableObjectDBPath );
            if ( Validator.Reports.Count > 0 ) Debug.LogError( $"[{nameof(ControlWindow)}] GDDB validation errors detected, count {Validator.Reports.Count}" );
            if ( AssetDatabase.Contains( soDB ) )
            {
                Debug.Log( $"[{nameof(ControlWindow)}] GDDB saved to scriptable object {updateSettings.ScriptableObjectDBPath}", soDB );
                EditorGUIUtility.PingObject( soDB );
            }
        }

        private void SaveGDDBToJson( UpdateDBSettings updateSettings )
        {
            if ( String.IsNullOrEmpty( updateSettings.JsonDBPath ) ) throw new InvalidOperationException( "Output path is empty" );

            var editorDB        = GDBEditor.GDB;
            var serializer      = new DBJsonSerializer();
            var assetsReference = CreateInstance<DirectAssetReferences>();
            var jsonDBStr       = serializer.Serialize( editorDB.RootFolder, editorDB.AllObjects, assetsReference );
            File.WriteAllText( updateSettings.JsonDBPath, jsonDBStr );
            Debug.Log( $"[{nameof(ControlWindow)}] GDDB json file saved to {updateSettings.JsonDBPath}" );
            EditorUtility.RevealInFinder( updateSettings.JsonDBPath );

            if ( !String.IsNullOrEmpty( updateSettings.JsonAssetsReferencePath ) )
            {
                AssetDatabase.CreateAsset( assetsReference, updateSettings.ScriptableObjectDBPath );
                Debug.Log( $"[{nameof(ControlWindow)}] GDDB assets reference saved to {updateSettings.JsonAssetsReferencePath}" );
            }

            if ( Validator.Reports.Count > 0 ) Debug.LogError( $"[{nameof(ControlWindow)}] GDDB validation errors detected, count {Validator.Reports.Count}" );
        }

        private void UpdateGDBInfo( )
        {
            //Can be called before GUI is created from event handler
            if( _dbStatsLbl == null )
                return;

            var rootFolder   = GDBEditor.GDB.RootFolder;
            var rootFullPath = AssetDatabase.GUIDToAssetPath( rootFolder.FolderGuid.ToString("N") );
            _dbStatsLbl.text = $"GDB root folder: {rootFullPath}, objects {GDBEditor.AllObjects.Count}, folders {GDBEditor.AllFolders.Count}";
            _dbHashLbl.text  = "DB structure hash: " + rootFolder.GetFoldersStructureChecksum( );
        }

        private void UpdateSourceGenInfo( )
        {
            //Can be called before GUI is created somehow
            if( _generatedStructureHashLbl == null )
                return;

            var rootFolderHash        = GDBEditor.GDB.RootFolder.GetFoldersStructureChecksum();
            var generatedCodeHash = GDBSourceGenerator.GetGeneratedCodeChecksum();
            _generatedStructureHashLbl.text = "Generated source hash: " + generatedCodeHash;
            _sourceGenHashIcon.style.backgroundImage = rootFolderHash == generatedCodeHash ? Resources.HashOkIcon : Resources.HashNotOkIcon;
        }

        private static class Resources
        {
            public static readonly Texture2D HashOkIcon    = UnityEngine.Resources.Load<Texture2D>( "check_24dp" );
            public static readonly Texture2D HashNotOkIcon = UnityEngine.Resources.Load<Texture2D>( "error_24dp" );
        }
    }
}