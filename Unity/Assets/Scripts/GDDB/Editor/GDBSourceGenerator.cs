using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using GDDB.Serialization;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    /// <summary>
    /// Trigger GDB source generation by updating json additional file. Source code generation makes DB folder access simpler. Trigger events: GD assets changed, play mode entered, build started etc
    /// </summary>
    [InitializeOnLoad]
    public static class GDBSourceGenerator
    {
        //Additional file to generate sources from
        private static readonly String  GDDBStructureFilePath = $"{Application.dataPath}/../Library/GDDBTreeStructure.json";
        private static SourceGeneratorSettings Settings => SourceGeneratorSettings.instance;

        public static event Action SourceUpdated;

        static GDBSourceGenerator( )
        {
            GDAssets.GDDBAssetsChanged.Subscribe( 5, OnGddbStructureChanged );
            EditorApplication.focusChanged += EditorApplicationOnfocusChanged;
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            GDDBUpdater.BuildPreprocessing += BuildPreprocessorOnBuildPreprocessing;
            Settings.Changed += SettingChanged;
        }

        private static void SettingChanged( SourceGeneratorSettings settings )
        {
            if( settings.AutoGenerateOnSourceChanged )
                GenerateGDBSource();        //Its check changes inside
        }

        private static void BuildPreprocessorOnBuildPreprocessing( )
        {
            if( Settings.AutoGenerateOnBuild )
                GenerateGDBSource(  );
        }

        private static void EditorApplicationOnplayModeStateChanged( PlayModeStateChange state )
        {
            if( Settings.AutoGenerateOnPlayMode && state == PlayModeStateChange.ExitingEditMode )
                GenerateGDBSource(  );
        }

        private static void EditorApplicationOnfocusChanged( Boolean isFocused )
        {
            if( Settings.AutoGenerateOnFocusLost && !isFocused )
                GenerateGDBSource(  );
        }

        private static void OnGddbStructureChanged(IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> deletedObjects )
        {
            if( Settings.AutoGenerateOnSourceChanged )
                GenerateGDBSource(  );
        }

        public static void GenerateGDBSource( Boolean forceRegenerate = false )
        {
            var databaseHash = GDBEditor.GDB.RootFolder.GetFoldersChecksum();
            var generatedHash = GetGeneratedCodeChecksum();
            if( databaseHash != generatedHash || forceRegenerate )
            {
                Debug.Log( $"[{nameof(GDBSourceGenerator)}]-[{nameof(GenerateGDBSource)}] db hash {databaseHash}, generated code hash {generatedHash}, force mode {forceRegenerate}" );

                //Update source generator data file
                var serializer = new FoldersJsonSerializer();
                var json = serializer.Serialize( GDBEditor.GDB.RootFolder, null, databaseHash ).ToString();
                File.WriteAllText( GDDBStructureFilePath, json );

                SourceUpdated?.Invoke();

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

        public static UInt64 GetGeneratedCodeChecksum( )
        {
            if ( !File.Exists( GDDBStructureFilePath ) )
                return 0;

            var json    = File.ReadAllText( GDDBStructureFilePath );
            var jObject = (JObject)JToken.Parse( json );
            if( jObject.ContainsKey( "hash") )
                return (UInt64)jObject["hash"].Value<BigInteger>();

            return 0;
        }
    }
}