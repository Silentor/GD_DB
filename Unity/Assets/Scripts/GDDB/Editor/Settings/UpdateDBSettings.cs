using System;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor
{
    [FilePath("ProjectSettings/GDDB/UpdateDBSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UpdateDBSettings : ScriptableSingleton<UpdateDBSettings>
    {
        public Boolean AutoUpdateOnRun          = true;
        public Boolean AutoUpdateOnBuild        = true;
        public Boolean ValidateDBOnBuild        = true;
        public Boolean UpdateScriptableObjectDB = true;
        public String  ScriptableObjectDBPath   = "Assets/Resources/DefaultGDDB.asset";
        public Boolean UpdateBinaryDB           = true;
        public String  BinaryDBPath               = "Assets/StreamingAssets/DefaultGDDB.bin";
        public Boolean UpdateJsonDB             = true;
        public String  JsonDBPath               = "Assets/StreamingAssets/DefaultGDDB.json";
        public Int32   JsonDBEditorIndent       = 4;
        public Int32   JsonDBPlayerIndent       = 0;
        public String  AssetsReferencePath  = "Assets/Resources/DefaultGDDBAssetsRef.asset";

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

    }
}