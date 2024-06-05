using System;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    //[CreateAssetMenu( fileName = "FILENAME", menuName = "MENUNAME", order = 0 )]
    [FilePath("GDDB/StressTestSettings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class StressTestSettings : ScriptableSingleton<StressTestSettings>
    {
        [Header("Scripts")]
        public String OutputFolder                  = "Assets/GeneratedGDDB/Scripts/";
        public String RootNamespace                  = "GeneratedGDDB";
        [Min(1)]
        public Int32 ComponentScriptsCount          = 500;
        [Min(1)]
        public Int32 ComponentNamespacesCount       = 100;

        public void Save( )
        {
            Save( true );
        }

    }
}