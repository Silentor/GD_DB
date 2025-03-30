using System;
using System.Net.NetworkInformation;
using GDDB.Editor.Validations;
using GDDB.Serialization;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    public static class MainMenuIntegration
    {
        //public const String Prefix = "Tools/GDDB/";           //Unity recommends to use "Tools" prefix for custom tools
        public const String Prefix = "GDDB/";

        [MenuItem( Prefix + "Open Control Window", priority = 0 )]
        private static void ShowControlWindow( )
        {
            var window = EditorWindow.GetWindow<ControlWindow>();
            window.titleContent = new GUIContent( "GDDB Control Window" );
            window.Show();
        }

        [MenuItem( Prefix + "Open Validate window...", priority = 1)]
        private static void Validate( )
        {
            ValidatorWindow.Open();
        }
        
        [MenuItem( Prefix + "Open create GDObject wizard...", priority = 2)]
        private static void CreateGDObject( )
        {
            var wnd = EditorWindow.GetWindow<CreateGDObjectWizard>();
            wnd.titleContent = new GUIContent("Create GDObject wizard", Icons.GDObjectIcon );
        }

        //Mostly debug tools

        [MenuItem( Prefix + "Print project hierarchy" )]
        private static void PrintHierarchyToConsole( )
        {
            var  parser  = new GdDbAssetsParser();
            if ( parser.Root != null )
            {
                Debug.Log( $"Root folder: {parser.RootFolderPath}, folder checksum {parser.Root.GetFoldersChecksum()}" );
                var       hierarchyStr = parser.Root.ToHierarchyString();
                using var stringReader = new System.IO.StringReader( hierarchyStr );
                while ( stringReader.ReadLine() is { } line )
                {
                    Debug.Log( line );
                }
            }
        }

        [MenuItem( Prefix + "Print hierarchy from file.." )]
        private static void LoadPrintHierarchyToConsole( )
        {
            var oldFolderKey       = $"{nameof(MainMenuIntegration)}.{nameof(LoadPrintHierarchyToConsole)}.OldFolder";

            var oldFolder = PlayerPrefs.GetString( oldFolderKey, "Assets" );
            var filePath = EditorUtility.OpenFilePanel( "Open GDDB json", oldFolder, "json,bin" );
            if ( String.IsNullOrEmpty( filePath ) )
                return;
            PlayerPrefs.SetString( oldFolderKey, System.IO.Path.GetDirectoryName( filePath ) );

            GdDb gddb;
            if( filePath.EndsWith( ".bin" ) )
            {
                using var fileStream = new System.IO.FileStream( filePath, System.IO.FileMode.Open );
                var gdLoader = new GdFileLoader( fileStream );
                gddb = gdLoader.GetGameDataBase();
            }
            else if( filePath.EndsWith( ".json" ) )
            {
                using var fileReader = new System.IO.StreamReader( filePath );
                var       gdLoader   = new GdFileLoader( fileReader );
                gddb = gdLoader.GetGameDataBase();
            }
            else
            {
                Debug.LogError( $"Unknown file extension {filePath}" );
                return;
            }

            var rootFolder = gddb.RootFolder;
            var       hierarchyStr = rootFolder.ToHierarchyString();
            using var stringReader = new System.IO.StringReader( hierarchyStr );
            Debug.Log( $"GDDB read from {filePath}, loaded hash {gddb.Hash}" );
            while ( stringReader.ReadLine() is { } line )
            {
                Debug.Log( line );
            }
        }

        [MenuItem( Prefix + "Open data file viewer" )]
        private static void ShowWindow( )
        {
            var window = EditorWindow.GetWindow<DataFileViewer>();
            window.titleContent = new GUIContent( "Data file viewer" );
            window.Show();
        }

        [MenuItem( Prefix + "Open GDDB test generator window" )]
        private static void ShowGeneratorWindow( )
        {
            var window = EditorWindow.GetWindow<StressTestDBGenerator>();
            window.titleContent = new GUIContent( "GDDB Generator" );
            window.Show();
        }


    }
}