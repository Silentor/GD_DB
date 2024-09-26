using System;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using Object = System.Object;

namespace GDDB.Editor
{
    public class ControlWindow : EditorWindow
    {
        private Label  _foldersInfoLbl;
        private Label  _foldersStructureHashLbl;
        private Label  _generatedStructureHashLbl;
        private Toggle _autoGenerateOnSourceChanged;

        private static readonly String GDDBStructureFilePath = $"{Application.dataPath}/../Library/GDDBTreeStructure.json";

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
            var generatedCodeHash = 0;
            if ( System.IO.File.Exists( GDDBStructureFilePath ) )
            {
                var jsonString    = System.IO.File.ReadAllText( GDDBStructureFilePath );
                var structureJObj = JObject.Parse( jsonString );
                var hash = structureJObj["hash"].Value<Int32>();
                generatedCodeHash = hash;
            }

            _generatedStructureHashLbl.text = "Generated  hash: " + generatedCodeHash;
        }
    }
}