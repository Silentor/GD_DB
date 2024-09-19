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
        private Label _foldersStructureHashLbl;
        private Label _generatedStructureHashLbl;

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
            GDOAssetPostprocessor.GddbStructureChanged += OnGddbStructureChanged;
        }

        private void OnGddbStructureChanged( )
        {
            var foldersParser = new FoldersParser();
            foldersParser.Parse();
            UpdateFoldersStructureHash( foldersParser.Root );
        }

        private void OnDisable( )
        {
            GDOAssetPostprocessor.GddbStructureChanged -= OnGddbStructureChanged;
        }

        private void CreateGUI( )
        {
            var foldersParser = new FoldersParser();
            foldersParser.Parse();

            var rootFolder = foldersParser.Root;
            _foldersStructureHashLbl = new Label(  );
            UpdateFoldersStructureHash( rootFolder );
            rootVisualElement.Add( _foldersStructureHashLbl );

            _generatedStructureHashLbl = new Label(  );
            UpdateGeneratedStructureHash(  );
            rootVisualElement.Add( _generatedStructureHashLbl );

            var generateBtn = new Button( GenerateGDDBSource );
            generateBtn.text = "Generate GDDB Source";
            rootVisualElement.Add( generateBtn );

        }

        private void GenerateGDDBSource( )
        {
            var gddbSourceFile = AssetDatabase.FindAssets( "t:asmdef GDDB" );
            if( gddbSourceFile.Length == 0 )
            {
                Debug.LogError( "GDDB assembly definition not found" );
                return;
            }
            var startTime = System.DateTime.Now;
            var path = AssetDatabase.GUIDToAssetPath( gddbSourceFile[0] );
            AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );
        }

        private void UpdateFoldersStructureHash( Folder rootFolder)
        {
            _foldersStructureHashLbl.text = "Folders structure hash: " + rootFolder.GetFoldersStructureHash( );
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

            _generatedStructureHashLbl.text = "Generated source hash: " + generatedCodeHash;
        }
    }
}