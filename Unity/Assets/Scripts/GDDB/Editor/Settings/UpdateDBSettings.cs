using System;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [FilePath("ProjectSettings/GDDB.SourceGenSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UpdateDBSettings : ScriptableSingleton<UpdateDBSettings>
    {
        public Boolean AutoUpdateOnRun          = true;
        public Boolean AutoUpdateOnBuild        = true;
        public Boolean ValidateDBOnBuild        = true;
        public Boolean UpdateScriptableObjectDB = true;
        public String  ScriptableObjectDBPath   = "Assets/Resources/DefaultGDDB.asset";
        public Boolean UpdateJsonDB             = true;
        public String  JsonDBPath               = "Assets/StreamingAssets/DefaultGDDB.json";
        public String  JsonAssetsReferencePath  = "Assets/Resources/DefaultGDDBAssetsRef.asset";

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