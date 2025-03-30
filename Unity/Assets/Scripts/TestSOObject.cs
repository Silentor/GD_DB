using System;
using System.Collections.Generic;
using GDDB;
using GDDB.Validations;
using UnityEngine;

namespace GDDB_User

{
    public class TestSOObject : ScriptableObject, IValidable
    {
        public Int32 SomeValue = 1;

        public GDObject             GDObjectReference;
        public TestSO2Object        SOObjectReference;
        public TestSOObject         SelfReference;

        public GdRef       GDObnjectRef;
        public GdFolder    FolderTest;
        public GdFolderRef FolderRef;

        public void Validate(GdFolder folder, GdDb db, List<ValidationReport> reports )
        {
            if( !GDObjectReference )
                reports.Add( new ValidationReport( folder, this, $"{nameof(GDObjectReference)} is not set " ) );
        }
    }
}