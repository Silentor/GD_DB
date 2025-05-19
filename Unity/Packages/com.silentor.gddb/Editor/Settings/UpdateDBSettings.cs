using System;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor
{
    [FilePath("ProjectSettings/Gddb/UpdateDBSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UpdateDBSettings : ScriptableSingleton<UpdateDBSettings>
    {
        public Boolean ValidateDBOnBuild        = true;
        public String  AssetsReferencePath  = "Assets/Resources/DefaultGDDBAssetsRef.asset";

        public DbSettings       ScriptableDBSettings = new DbSettings(){UpdateOnPlayerBuild = true, UpdateOnEditorRun = true, Path = "Assets/Resources/DefaultGDDB.asset"};
        public JsonDbSettings   JsonDBSettings = new JsonDbSettings(){Path = "Assets/StreamingAssets/DefaultGDDB.json"};
        public DbSettings       BinDBSettings = new DbSettings(){Path = "Assets/StreamingAssets/DefaultGDDB.bin"};

        private void OnDisable( )
        {
            Save();
        }

        private void OnDestroy( )
        {
            Save();
        }

        public void Save( )
        {
            Save( true );
        }

        [Serializable]
        public class DbSettings 
        {
            public Boolean UpdateOnEditorRun;
            public Boolean UpdateOnPlayerBuild;
            public String Path;
        }

        [Serializable]
        public class JsonDbSettings : DbSettings
        {
            public Int32 IndentOnEditorRun              = 4;
            public Int32 IndentOnDevelopmentPlayerBuild = 0;
            public Int32 IndentOnReleasePlayerBuild     = 4;
        }

        [CustomEditor( typeof(UpdateDBSettings) )]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI( )
            {
                GUILayout.Label( "Scriptable Object Gddb settings", EditorStyles.boldLabel );
                EditorGUILayout.HelpBox( "Gddb in Scriptable Object format. Fast and simple workflow. Should be referenced as Unity asset or loaded as Resources. But cannot be easily downloaded/copied/loaded as a common file.", MessageType.Info );
                var soDBSettings = serializedObject.FindProperty( nameof( UpdateDBSettings.ScriptableDBSettings ) );
                EditorGUILayout.PropertyField( soDBSettings.FindPropertyRelative( nameof(DbSettings.UpdateOnEditorRun) ) );
                EditorGUILayout.PropertyField( soDBSettings.FindPropertyRelative( nameof(DbSettings.UpdateOnPlayerBuild) ) );
                EditorGUILayout.PropertyField( soDBSettings.FindPropertyRelative( nameof(DbSettings.Path) ) );
                Resources.VerticalSpace();

                GUILayout.Label( "JSON Gddb settings", EditorStyles.boldLabel );
                EditorGUILayout.HelpBox( "Gddb in JSON format. Easy to read (when indented). Should be downloaded and loaded as a common file (for example from StreamingAssets)", MessageType.Info );
                var jsonDBSettings = serializedObject.FindProperty( nameof( UpdateDBSettings.JsonDBSettings ) );
                EditorGUILayout.PropertyField( jsonDBSettings.FindPropertyRelative( nameof(DbSettings.UpdateOnEditorRun) ) );
                EditorGUILayout.PropertyField( jsonDBSettings.FindPropertyRelative( nameof(JsonDbSettings.IndentOnEditorRun) ) );
                EditorGUILayout.PropertyField( jsonDBSettings.FindPropertyRelative( nameof(DbSettings.UpdateOnPlayerBuild) ) );
                EditorGUILayout.PropertyField( jsonDBSettings.FindPropertyRelative( nameof(JsonDbSettings.IndentOnDevelopmentPlayerBuild) ) );
                EditorGUILayout.PropertyField( jsonDBSettings.FindPropertyRelative( nameof(JsonDbSettings.IndentOnReleasePlayerBuild) ) );
                EditorGUILayout.PropertyField( jsonDBSettings.FindPropertyRelative( nameof(DbSettings.Path) ) );
                Resources.VerticalSpace();

                GUILayout.Label( "Binary Object Gddb settings", EditorStyles.boldLabel );
                EditorGUILayout.HelpBox( "Gddb in custom binary format. Compact and fast to load. Should be downloaded and loaded as a common file (for example from StreamingAssets)", MessageType.Info );
                var binDBSettings = serializedObject.FindProperty( nameof( UpdateDBSettings.BinDBSettings ) );
                EditorGUILayout.PropertyField( binDBSettings.FindPropertyRelative( nameof(DbSettings.UpdateOnEditorRun) ) );
                EditorGUILayout.PropertyField( binDBSettings.FindPropertyRelative( nameof(DbSettings.UpdateOnPlayerBuild) ) );
                EditorGUILayout.PropertyField( binDBSettings.FindPropertyRelative( nameof(DbSettings.Path) ) );
                Resources.VerticalSpace();

                GUILayout.Label( "Common settings", EditorStyles.boldLabel );
                EditorGUILayout.PropertyField( serializedObject.FindProperty( nameof(UpdateDBSettings.ValidateDBOnBuild) ) );
                EditorGUILayout.PropertyField( serializedObject.FindProperty( nameof(UpdateDBSettings.AssetsReferencePath) ) );
            }

            private static class Resources
            {
                public static readonly GUIStyle VerticalSpaceStyle = new GUIStyle() { fixedHeight = 4 };

                public static void VerticalSpace( )
                {
                    GUILayout.Label( "", VerticalSpaceStyle );
                } 
            }
        }


    }

}