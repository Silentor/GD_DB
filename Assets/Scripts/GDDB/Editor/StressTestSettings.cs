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
        public String OutputFolderComponents         = "Assets/GeneratedGDDB/Components/";
        public String RootNamespace                  = "GeneratedGDDB";
        [Min(1)]
        public Int32 ComponentScriptsCount          = 500;
        [Min(1)]
        public Int32 ComponentNamespacesCount       = 50;

        [Header("Categories")]
        public String OutputFolderCategories            = "Assets/GeneratedGDDB/Categories/";
        public Int32 CategoriesCount                    = 100;
        public Int32 CategoryItemsCount                 = 1000;


        [Header("GD Objects")]
        public String OutputFolderDB                = "Assets/GeneratedGDDB/DB/";
        [Min(1)]
        public Int32 GDObjectsCount                 = 10000;

        public void Save( )
        {
            Save( true );
        }

    }
}