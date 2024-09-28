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
        private Label  _foldersInfoLbl;
        private Label  _foldersStructureHashLbl;
        private Label  _generatedStructureHashLbl;
        private Toggle _autoGenerateOnSourceChanged;
        private Toggle _autoGenerateOnFocusLost;
        private Toggle _autoGenerateOnEnterPlayMode;
        private Toggle _autoGenerateOnBuild;

        [MenuItem( "GDDB/Control Window" )]
        private static void ShowWindow( )
        {
            var window = GetWindow<ControlWindow>();
            window.titleContent = new GUIContent( "GDDB Control Window" );
            window.Show();
        }

        private void OnEnable( )
        {
            AssetPostprocessor.GDBStructureChanged.Subscribe( 10, OnGddbStructureChanged );
        }

        private void OnGddbStructureChanged( )
        {
            UpdateFoldersStructureHash( );
        }

        private void OnDisable( )
        {
            AssetPostprocessor.GDBStructureChanged.Unsubscribe( OnGddbStructureChanged );
        }

        private void CreateGUI( )
        {
            _foldersInfoLbl = new Label(  );
            _foldersStructureHashLbl = new Label(  );
            UpdateFoldersStructureHash(   );
            rootVisualElement.Add( _foldersInfoLbl );
            rootVisualElement.Add( _foldersStructureHashLbl );

            _generatedStructureHashLbl = new Label(  );
            UpdateGeneratedStructureHash(  );
            rootVisualElement.Add( _generatedStructureHashLbl );

            _autoGenerateOnSourceChanged       = new Toggle( "Auto generate on source changed" );
            _autoGenerateOnSourceChanged.value = GDBSourceGenerator.Settings.AutoGenerateOnSourceChanged;
            _autoGenerateOnSourceChanged.RegisterValueChangedCallback( evt => GDBSourceGenerator.Settings.AutoGenerateOnSourceChanged = evt.newValue );
            rootVisualElement.Add( _autoGenerateOnSourceChanged );

            _autoGenerateOnFocusLost       = new Toggle( "Auto generate on editor focus lost" );
            _autoGenerateOnFocusLost.value = GDBSourceGenerator.Settings.AutoGenerateOnFocusLost;
            _autoGenerateOnFocusLost.RegisterValueChangedCallback( evt => GDBSourceGenerator.Settings.AutoGenerateOnFocusLost = evt.newValue );
            rootVisualElement.Add( _autoGenerateOnFocusLost );

            _autoGenerateOnEnterPlayMode       = new Toggle( "Auto generate on enter play mode" );
            _autoGenerateOnEnterPlayMode.value = GDBSourceGenerator.Settings.AutoGenerateOnPlayMode;
            _autoGenerateOnEnterPlayMode.RegisterValueChangedCallback( evt => GDBSourceGenerator.Settings.AutoGenerateOnPlayMode = evt.newValue );
            rootVisualElement.Add( _autoGenerateOnEnterPlayMode );

            _autoGenerateOnBuild       = new Toggle( "Auto generate on build" );
            _autoGenerateOnBuild.value = GDBSourceGenerator.Settings.AutoGenerateOnBuild;
            _autoGenerateOnBuild.RegisterValueChangedCallback( evt => GDBSourceGenerator.Settings.AutoGenerateOnBuild = evt.newValue );
            rootVisualElement.Add( _autoGenerateOnBuild );


            var generateBtn = new Button( GenerateGDDBSource );
            generateBtn.style.width = 200;
            generateBtn.text = "Generate GDDB Source";
            rootVisualElement.Add( generateBtn );

        }

        private void GenerateGDDBSource( )
        {
            GDBSourceGenerator.GenerateGDBSource( );
        }

        private void UpdateFoldersStructureHash( )
        {
            var rootFolder = GDBEditor.GDB.RootFolder;
            _foldersInfoLbl.text          = $"GDB root folder: {rootFolder.Path}, objects {GDBEditor.AllObjects.Count}, folders {GDBEditor.AllFolders.Count}";
            _foldersStructureHashLbl.text = "Source hash: " + rootFolder.GetFoldersStructureHash( );
        }

        private void UpdateGeneratedStructureHash( )
        {
            var generatedCodeHash = GDBSourceGenerator.GetGeneratedCodeHash();
            _generatedStructureHashLbl.text = "Generated  hash: " + generatedCodeHash;
        }
    }
}