using System;
using UnityEditor;

namespace GDDB.Editor
{
    /// <summary>
    /// Watch for changes in GDDB structure and update source generated code
    /// </summary>
    [InitializeOnLoad]
    public static class GDBSourceGeneratorDaemon
    {
        public static Boolean GenerateOnGDDBChange { get; set; } = true;

        public static Int32 FoldersStructureHash    { get; private set; } 
        public static Int32 GeneratedCodeHash       { get; private set; } 
        
        static GDBSourceGeneratorDaemon( )
        {
            AssetPostprocessor.GddbStructureChanged += OnGddbStructureChanged;
        }

        private static void OnGddbStructureChanged( )
        {

        }
    }
}