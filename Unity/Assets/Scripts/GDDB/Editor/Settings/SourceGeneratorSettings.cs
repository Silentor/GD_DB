using System;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [FilePath("ProjectSettings/GDDB.SourceGenSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SourceGeneratorSettings : ScriptableSingleton<SourceGeneratorSettings>
    {
        public Boolean AutoGenerateOnSourceChanged;
        public Boolean AutoGenerateOnPlayMode  = true;
        public Boolean AutoGenerateOnBuild     = true;
        public Boolean AutoGenerateOnFocusLost = true;

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