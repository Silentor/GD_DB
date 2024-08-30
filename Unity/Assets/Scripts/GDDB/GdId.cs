using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GDDB
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct GdId
    {
        [FieldOffset(0)]
        public Guid   GUID;
        [FieldOffset(0)]
        public UInt64 Serializalble1;
        [FieldOffset(8)]
        public UInt64 Serializalble2;
    }
}
