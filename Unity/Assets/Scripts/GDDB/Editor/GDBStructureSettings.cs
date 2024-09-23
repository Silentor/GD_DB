using System;
using UnityEditor;

namespace GDDB.Editor
{
    public static class GDBStructureSettings
    {
        public static event Action OnSettingsChanged;

        public static Boolean GenerateOnGDBChange
        {
            get => EditorPrefs.GetBool( GenerateOnGDBChangeKey, false );
            set
            {
                EditorPrefs.SetBool( GenerateOnGDBChangeKey, value ); 
                OnSettingsChanged?.Invoke();
            }
        }

        public static Boolean GenerateBeforePlayMode
        {
            get => EditorPrefs.GetBool( GenerateBeforePlayModeKey, true );
            set
            {
                EditorPrefs.SetBool( GenerateBeforePlayModeKey, value ); 
                OnSettingsChanged?.Invoke();
            }
        }

        public static Boolean GenerateBeforeBuild
        {
            get => EditorPrefs.GetBool( GenerateBeforeBuildKey, true );
            set
            {
                EditorPrefs.SetBool( GenerateBeforeBuildKey, value ); 
                OnSettingsChanged?.Invoke();
            }
        }
        

        private static readonly String GenerateOnGDBChangeKey = String.Concat( nameof(GDBStructureSettings), ".", nameof(GenerateOnGDBChange) );
        private static readonly String GenerateBeforePlayModeKey = String.Concat( nameof(GDBStructureSettings), ".", nameof(GenerateBeforePlayMode) );
        private static readonly String GenerateBeforeBuildKey = String.Concat( nameof(GDBStructureSettings), ".", nameof(GenerateBeforeBuild) );
    }
}