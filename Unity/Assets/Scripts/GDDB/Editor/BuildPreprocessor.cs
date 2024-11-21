using System;
using GDDB.Serialization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GDDB.Editor
{
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public Int32 callbackOrder { get; } = 0;

        public static event Action BuildPreprocessing;

        public void  OnPreprocessBuild( BuildReport report )
        {
            BuildPreprocessing?.Invoke();

            var editorDB   = GDBEditor.GDB;
            var serializer = new DBAssetSerializer();
            var rootFolder = editorDB.RootFolder;
            var asset      = serializer.Serialize( rootFolder );
            var path       = $"Assets/Resources/{editorDB.Name}.folders.asset";
            AssetDatabase.CreateAsset( asset, path);

            Debug.Log( $"[{nameof(BuildPreprocessor)}]-[{nameof(OnPreprocessBuild)}] Saved GDDB to asset {path}" );
        }
    }
}
