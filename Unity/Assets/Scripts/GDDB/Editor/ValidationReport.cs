﻿using System;

namespace GDDB
{
    public class ValidationReport
    {
        public ValidationReport( GdFolder folder, GDObject gdo, String message )
        {
            Folder    = folder;
            GdObject  = gdo;
            Message   = message;
            //IsWarning = isWarning;
        }

        public GdFolder       Folder      { get; }
        public GDObject     GdObject    { get; }
        public String       Message     { get; }
        //public Boolean IsWarning { get; }

        public override String ToString( )
        {
            return $"{Folder.GetPath()}/{GdObject.Name}: {Message}";
        }
    }
}