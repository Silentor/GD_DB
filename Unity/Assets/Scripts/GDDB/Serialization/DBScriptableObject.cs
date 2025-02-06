using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB.Serialization
{
    public class DBScriptableObject : ScriptableObject
    {
        public UInt64                   Hash;
        public List<SerializableFolder> Folders;
    }

    [Serializable]
    public struct SerializableFolder
    {
        public String                   Name;
        public SerializableGuid         Guid;
        public Int32                    Depth;
        public List<ScriptableObject>   Objects;
    }
}