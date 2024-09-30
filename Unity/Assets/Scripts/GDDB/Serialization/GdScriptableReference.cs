using System;
using System.Collections.Generic;
using GDDB.GDDB;
using UnityEngine;

namespace GDDB
{                                 
    public class GdScriptableReference : ScriptableObject
    {
        public GDObjectAndGuid[]       Content;
    }

    [Serializable]
    public struct GDObjectAndGuid
    {
        public SerializableGuid Guid;               //todo Benchmark String Guid vs 2 ulong Guid
        public GDObject         Object;
    }
}