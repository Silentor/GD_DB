using System;
using UnityEngine;

namespace GDDB
{
    public class ValidationReport
    {
        public ValidationReport( GdFolder folder, ScriptableObject gdo, String propertyPath, String message )
        {
            PropertyPath = propertyPath;
            Folder                         = folder;
            GdObject                       = gdo;
            Message                        = message;
            //IsWarning = isWarning;
        }

        public ValidationReport( GdFolder folder, ScriptableObject gdo, String message ) : this ( folder, gdo, null, message ){}

        public readonly GdFolder         Folder  ;
        public readonly ScriptableObject GdObject;
        public readonly String           PropertyPath;
        public readonly String  Message  ;
        //public Boolean IsWarning { get; }

        public String GetLocation( )
        {
            if( String.IsNullOrEmpty( PropertyPath ) )
                return $"{Folder.GetPath()}/{GdObject.name}";
            return $"{Folder.GetPath()}/{GdObject.name}.{PropertyPath}";
        }

        public override String ToString( )
        {
            return $"{GetLocation()}: {Message}";
        }
    }
}