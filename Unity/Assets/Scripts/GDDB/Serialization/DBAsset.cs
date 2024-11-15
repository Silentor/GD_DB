﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB.Serialization
{
    public class DBAsset : ScriptableObject
    {
        public List<SerializableFolder> Folders;
    }

    [Serializable]
    public struct SerializableFolder
    {
        public String           Name;
        public SerializableGuid Guid;
        public Int32            Depth;
        public List<GDObject>   Objects;
    }
}