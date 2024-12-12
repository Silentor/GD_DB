using System;
using GDDB.Serialization;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    public static class MainMenuIntegration
    {
        [MenuItem( "GDDB/Open Control Window", priority = 0 )]
        private static void ShowControlWindow( )
        {
            var window = EditorWindow.GetWindow<ControlWindow>();
            window.titleContent = new GUIContent( "GDDB Control Window" );
            window.Show();
        }

        [MenuItem( "GDDB/Open GDDB test generator window", priority = 1 )]
        private static void ShowWindow( )
        {
            var window = EditorWindow.GetWindow<StressTestDBGenerator>();
            window.titleContent = new GUIContent( "GDDB Generator" );
            window.Show();
        }

        [MenuItem( "GDDB/Print project hierarchy", priority = 2 )]
        private static void PrintHierarchyToConsole( )
        {
            var  parser  = new FoldersParser();
            if ( parser.Parse() )
            {
                Debug.Log( $"Root folder: {parser.Root.GetPath()}, folder checksum {parser.Root.GetFoldersChecksum()}" );
                var       hierarchyStr = parser.Root.ToHierarchyString();
                using var stringReader = new System.IO.StringReader( hierarchyStr );
                while ( stringReader.ReadLine() is { } line )
                {
                    Debug.Log( line );
                }
            }
        }

        [MenuItem( "GDDB/Load and print hierarchy..", priority = 3 )]
        private static void LoadPrintHierarchyToConsole( )
        {
            var oldFolderKey       = $"{nameof(MainMenuIntegration)}.{nameof(LoadPrintHierarchyToConsole)}.OldFolder";

            var oldFolder = PlayerPrefs.GetString( oldFolderKey, "Assets" );
            var jsonPath = EditorUtility.OpenFilePanel( "Open GDDB json", oldFolder, "json" );
            if ( String.IsNullOrEmpty( jsonPath ) )
                return;
            PlayerPrefs.SetString( oldFolderKey, System.IO.Path.GetDirectoryName( jsonPath ) );

            using var strReader    = new System.IO.StreamReader( jsonPath );
            using var jsonReader   = new Newtonsoft.Json.JsonTextReader( strReader );
            var       folderReader = new FoldersJsonSerializer();
            var       rootFolder   = folderReader.Deserialize( jsonReader, null, out var hash );
            var       hierarchyStr = rootFolder.ToHierarchyString();
            using var stringReader = new System.IO.StringReader( hierarchyStr );
            Debug.Log( $"GDDB read from {jsonPath}, hash {hash}" );
            while ( stringReader.ReadLine() is { } line )
            {
                Debug.Log( line );
            }
        }
    }
}