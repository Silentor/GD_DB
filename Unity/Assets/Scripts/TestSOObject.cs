﻿using System;
using GDDB;
using UnityEngine;

namespace GDDB_User

{
    [CreateAssetMenu( fileName = "TestSOObject", menuName = "GDDB/TestSOObject", order = 0 )]
    public class TestSOObject : ScriptableObject
    {
        public Int32 SomeValue = 1;

        public GDObject             GDObjectReference;
        public TestSO2Object        SOObjectReference;
        public TestSOObject         SelfReference;

        public GdRef       GDObnjectRef;
        public GdFolder    FolderTest;
        public GdFolderRef FolderRef;
    }
}