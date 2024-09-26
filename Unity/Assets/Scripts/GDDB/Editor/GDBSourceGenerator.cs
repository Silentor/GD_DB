using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    /// <summary>
    /// Trigger GDB source generation if GD assets changed, play mode entered, build started etc
    /// </summary>
    [InitializeOnLoad]
    public static class GDBSourceGenerator
    {
        static GDBSourceGenerator( )
        {
            AssetPostprocessor.GDBStructureChanged.Subscribe( 5, OnGddbStructureChanged );
        }

        private static void OnGddbStructureChanged( )
        {
            if( Settings.AutoGenerateOnSourceChanged )
                GenerateGDBSource(  );
        }

        public static void GenerateGDBSource( Boolean forceRegenerate = false )
        {
            var databaseHash = GDBEditor.GDB.RootFolder.GetFoldersStructureHash();
            var generatedHash = GetGeneratedCodeHash();
            if( databaseHash != generatedHash || forceRegenerate )
            {
                Debug.Log( $"GDBSourceGenerator: GenerateGDBSource" );

                //Update source generator data file
                var serializer = new FoldersSerializer();
                var json = serializer.Serialize( GDBEditor.GDB.RootFolder, databaseHash );
                File.WriteAllText( GDDBStructureFilePath, json );

                //Trigger recompile of GDDB assembly and source generation
                var gddbSourceFile = AssetDatabase.FindAssets( "t:MonoScript GdDb" );

                foreach ( var gddbFileId in gddbSourceFile )
                {
                    var path      = AssetDatabase.GUIDToAssetPath( gddbFileId );
                    if ( path.EndsWith( "GdDb.cs" ) )
                    {
                        AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );
                        return;
                    }
                }

                Debug.LogError( "GDDB assembly definition not found" );
                //var startTime = System.DateTime.Now;
            }
        }

        public static Int32 GetGeneratedCodeHash( )
        {
            if ( !File.Exists( GDDBStructureFilePath ) )
                return 0;

            var json    = File.ReadAllText( GDDBStructureFilePath );
            var jObject = JObject.Parse( json );
            if( jObject.ContainsKey( "hash" ) )
                return jObject["hash"].Value<Int32>();

            return 0;
        }

        private static readonly String  GDDBStructureFilePath = $"{Application.dataPath}/../Library/GDDBTreeStructure.json";

        public static class Settings
        {
            private static readonly String AutoGenerateOnSourceChangeKey = $"{nameof(GDBSourceGenerator)}.{nameof(AutoGenerateOnSourceChanged)}";

            public static Boolean AutoGenerateOnSourceChanged
            {
                get => EditorPrefs.GetBool( AutoGenerateOnSourceChangeKey, false );
                set => EditorPrefs.SetBool( AutoGenerateOnSourceChangeKey, value );
            }
        }
    }
}