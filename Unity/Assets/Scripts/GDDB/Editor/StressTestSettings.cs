using System;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [FilePath("ProjectSettings/GDDB.StressTestSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class StressTestSettings : ScriptableSingleton<StressTestSettings>
    {
        [Header("Scripts")]
        public String OutputFolderComponents         = "Assets/GeneratedGDDB/Components";
        public String RootNamespace                  = "GeneratedGDDB";
        [Min(1)]
        public Int32 ComponentScriptsCount          = 500;
        [Min(1)]
        public Int32 ComponentNamespacesCount       = 50;

        [Header("GD Objects")]
        public String OutputFolderDB                = "Assets/GeneratedGDDB/DB";
        [Min(1)]
        public Int32 GDObjectsCount                 = 1000;
        [Min(1)]
        public Int32 MaxComponentsPerObject         = 5;

        [Min(0)]
        public Vector2Int SubfoldersCount    = new ( 0, 10 );
        [Min(1)]
        public Int32    SubfoldersMaxDepth = 5;
        [Min(1)]
        public Vector2Int ObjectsPerFolders    = new ( 1, 50 );

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