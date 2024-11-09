using System;

namespace GDDB
{
    public class ValidationReport
    {
        public ValidationReport( Folder folder, GDObject gdo, String message )
        {
            Folder    = folder;
            GdObject  = gdo;
            Message   = message;
            //IsWarning = isWarning;
        }

        public Folder       Folder      { get; }
        public GDObject     GdObject    { get; }
        public String       Message     { get; }
        //public Boolean IsWarning { get; }
    }
}