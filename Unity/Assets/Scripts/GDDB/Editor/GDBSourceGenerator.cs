using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using GDDB.Serialization;
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
        private static readonly String  GDDBStructureFileName = "Structure.GdDbSourceGen.additionalfile";
        private static readonly String  DefaultStructureFilePath = "Assets/Settings/Gddb";
        private static SourceGeneratorSettings Settings => SourceGeneratorSettings.instance;

        public static event Action SourceUpdated;

        static GDBSourceGenerator( )
        {
            GDAssetProcessor.GDDBAssetsChanged.Subscribe( 5, OnGddbStructureChanged );
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

        private static String GetSourceFilePath( )
        {
            var withoutExtension = Path.GetFileNameWithoutExtension( GDDBStructureFileName );
            var sourceFiles = AssetDatabase.FindAssets( withoutExtension );

            var results = new List<String>();
            foreach ( var sourceFileGuid in sourceFiles )
            {
                var path = AssetDatabase.GUIDToAssetPath( sourceFileGuid );
                if( Path.GetExtension( path ) == ".additionalfile" )
                    results.Add( path );
            }

            if( results.Count == 0 )
            {
                return DefaultStructureFilePath + GDDBStructureFileName;
            }

            if( results.Count > 1 )
            {
                Debug.LogError( $"[{nameof(GDBSourceGenerator)}]-[{nameof(GetSourceFilePath)}] Multiple source files found: {String.Join( ", ", results.ToArray() )}. Please keep only one source file {GDDBStructureFileName}" );
            }

            return results[0];
        }

        public static void GenerateGDBSource( Boolean forceRegenerate = false )
        {
            if ( EditorDB.DB == null )
            {
                //todo remove source gen input file if no db
                return;
            }

            var databaseHash = EditorDB.DB.RootFolder.GetFoldersChecksum();
            var generatedHash = GetGeneratedFileChecksum();
            if( databaseHash != generatedHash || forceRegenerate )
            {
                Debug.Log( $"[{nameof(GDBSourceGenerator)}]-[{nameof(GenerateGDBSource)}] db hash {databaseHash}, generated code hash {generatedHash}, force mode {forceRegenerate}" );

                //Update source generator data file
                var serializer = new FolderSerializer();
                var buffer = new StringBuilder();
                var writer = new JsonNetWriter( buffer, true );
                writer.WriteStartObject();
                writer.WritePropertyName( "hash" );
                writer.WriteValue( databaseHash );
                writer.WritePropertyName( "Root" );
                serializer.Serialize( EditorDB.DB.RootFolder, null, writer);
                writer.WriteEndObject();

                var sourceFilePath = GetSourceFilePath();
                var directory = Path.GetDirectoryName( sourceFilePath );
                if ( !Directory.Exists( directory ) )
                    Directory.CreateDirectory( directory );
                File.WriteAllText( sourceFilePath, buffer.ToString() );
                AssetDatabase.Refresh( ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport );

                Debug.Log( $"[{nameof(GDBSourceGenerator)}]-[{nameof(GenerateGDBSource)}] Updated source generator structure file {sourceFilePath}" );
                SourceUpdated?.Invoke();

                return;

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

        public static UInt64 GetGeneratedFileChecksum( )
        {
            var path = GetSourceFilePath();

            if ( !File.Exists( path ) )
                return 0;

            var reader = new JsonNetReader( File.ReadAllText( path ), false );
            while ( reader.Depth == 0 )
            {
                reader.ReadNextToken();
                if( reader.CurrentToken == EToken.PropertyName && reader.GetPropertyName() == "hash" )
                {
                    return reader.ReadUInt64Value();
                }
            }

            return 0;
        }

        public static UInt64 GetGeneratedCodeChecksum( )
        {
            var generatedCode = TypeCache.GetTypesWithAttribute<GeneratedCodeAttribute>();
            foreach ( var generatedCodeType in generatedCode )
            {
                var generatedAttr = generatedCodeType.GetCustomAttribute<GeneratedCodeAttribute>( );
                if( generatedAttr.Tool == "GdDbSourceGen" && UInt64.TryParse( generatedAttr.Version, out var version ) )
                {
                    return version;
                }
            }

            return 0;
        }

        public static void RemoveSourceFile( )
        {
            var sourceFilePath = GetSourceFilePath();
            if( File.Exists( sourceFilePath ) )
            {
                File.Delete( sourceFilePath );
                Debug.Log( $"[{nameof(GDBSourceGenerator)}]-[{nameof(RemoveSourceFile)}] Removed source generator structure file {sourceFilePath}" );
                AssetDatabase.Refresh();
            }
        }
    }
}