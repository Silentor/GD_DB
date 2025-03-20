using System;
using UnityEngine;

namespace GDDB
{
    public class ValidationReport
    {
        public ValidationReport( GdFolder folder, ScriptableObject gdo, String message )
        {
            Folder    = folder;
            GdObject  = gdo;
            Message   = message;
            //IsWarning = isWarning;
        }

        public GdFolder             Folder      { get; }
        public ScriptableObject     GdObject    { get; }
        public String               Message     { get; }
        //public Boolean IsWarning { get; }

        public override String ToString( )
        {
            return $"{Folder.GetPath()}/{GdObject.name}: {Message}";
        }
    }
}