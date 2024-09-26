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
    public static class GDBSourceGenerator
    {
        public static Boolean GenerateOnGDAssetsChange { get; set; } = true;

        //public static Int32 FoldersStructureHash    { get; private set; } 
        //public static Int32 GeneratedCodeHash       { get; private set; }

        static GDBSourceGenerator( )
        {
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
                var gddbSourceFile = AssetDatabase.FindAssets( "t:asmdef GDDB" );
                if( gddbSourceFile.Length == 0 )
                {
                    Debug.LogError( "GDDB assembly definition not found" );
                    return;
                }
                var startTime = System.DateTime.Now;
                var path      = AssetDatabase.GUIDToAssetPath( gddbSourceFile[0] );
                AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );
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
    }
}