using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace GDDB.Editor
{
    public class ControlWindow : EditorWindow
    {
        private Label         _foldersInfoLbl;
        private Label         _foldersStructureHashLbl;
        private Label         _generatedStructureHashLbl;
        private Toggle        _autoGenerateOnSourceChanged;
        private Toggle        _autoGenerateOnFocusLost;
        private Toggle        _autoGenerateOnEnterPlayMode;
        private Toggle        _autoGenerateOnBuild;
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
            Debug.Log( $"[{nameof(ControlWindow)}]-[{nameof(OnEnable)}] " );
            AssetPostprocessor.GDBStructureChanged.Subscribe( 10, OnGddbStructureChanged );
        }

        private void OnGddbStructureChanged( )
        {
            UpdateGDBInfo( );
            UpdateSourceGenInfo();
        }

        private void OnDisable( )
        {
            AssetPostprocessor.GDBStructureChanged.Unsubscribe( OnGddbStructureChanged );
        }

        private void CreateGUI( )
        {
            Debug.Log( $"[{nameof(ControlWindow)}]-[{nameof(CreateGUI)}] " );
            var window = UnityEngine.Resources.Load<VisualTreeAsset>( "ControlWindow" ).Instantiate();

            _foldersInfoLbl = window.Q<Label>( "GDBInfo" );
            _foldersStructureHashLbl = window.Q<Label>( "GDBHash" );
            

            //Source Gen settings
            _autoGenerateOnSourceChanged = SetupSettingsToggle(window, "GenerateOnChange", GDBSourceGenerator.Settings.AutoGenerateOnSourceChanged, val => GDBSourceGenerator.Settings.AutoGenerateOnSourceChanged = val );
            _autoGenerateOnFocusLost = SetupSettingsToggle(window, "GenerateOnFocus", GDBSourceGenerator.Settings.AutoGenerateOnFocusLost, val => GDBSourceGenerator.Settings.AutoGenerateOnFocusLost = val );
            _autoGenerateOnEnterPlayMode = SetupSettingsToggle(window, "GenerateOnPlayMode", GDBSourceGenerator.Settings.AutoGenerateOnPlayMode, val => GDBSourceGenerator.Settings.AutoGenerateOnPlayMode = val );
            _autoGenerateOnBuild = SetupSettingsToggle(window, "GenerateOnBuild", GDBSourceGenerator.Settings.AutoGenerateOnBuild, val => GDBSourceGenerator.Settings.AutoGenerateOnBuild = val );
            _generatedStructureHashLbl = window.Q<Label>( "SourceGenHash" );
            _sourceGenHashIcon = window.Q<VisualElement>( "SourceGenHashIcon" );
            _sourceGenBtn = window.Q<Button>( "SourceGenBtn" );
            _sourceGenBtn.clicked += GenerateSourceManual;

            UpdateGDBInfo();
            UpdateSourceGenInfo();

            rootVisualElement.Add( window );
        }

        private Toggle SetupSettingsToggle( VisualElement container, String toggleName, Boolean value, Action<Boolean> setter )
        {
            var result = container.Q<Toggle>( toggleName );
            result.value = value;
            result.RegisterValueChangedCallback( evt => setter( evt.newValue ) );
            return result;
        }

        private void GenerateSourceManual( )
        {
            GDBSourceGenerator.GenerateGDBSource( true );
        }

        private void UpdateGDBInfo( )
        {
            var rootFolder   = GDBEditor.GDB.RootFolder;
            var rootFullPath = AssetDatabase.GUIDToAssetPath( rootFolder.FolderGuid.ToString("N") );
            _foldersInfoLbl.text          = $"GDB root folder: {rootFolder.GetPath()}, objects {GDBEditor.AllObjects.Count}, folders {GDBEditor.AllFolders.Count}";
            _foldersStructureHashLbl.text = "Source hash: " + rootFolder.GetFoldersStructureHash( );
        }

        private void UpdateSourceGenInfo( )
        {
            var rootFolderHash        = GDBEditor.GDB.RootFolder.GetFoldersStructureHash();
            var generatedCodeHash = GDBSourceGenerator.GetGeneratedCodeHash();
            _generatedStructureHashLbl.text = "Generated  hash: " + generatedCodeHash;
            _sourceGenHashIcon.style.backgroundImage = rootFolderHash == generatedCodeHash ? Resources.HashOkIcon : Resources.HashNotOkIcon;
        }

        private static class Resources
        {
            public static readonly Texture2D HashOkIcon    = UnityEngine.Resources.Load<Texture2D>( "check_24dp" );
            public static readonly Texture2D HashNotOkIcon = UnityEngine.Resources.Load<Texture2D>( "error_24dp" );
        }
    }
}