#nullable enable
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GDDB
{
    /// <summary>
    /// Reference to a gd folder. Autoresolved inside GDDB, but needs manually resolved if used outside.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct GdFolderRef : IEquatable<GdFolderRef>, IComparable<GdFolderRef>
    {
        [FieldOffset(0)]
        private Guid   _mappedGuid;
        [FieldOffset(0)]
        [SerializeField] UInt64 guidPart1;
        [FieldOffset(8)]
        [SerializeField] UInt64 guidPart2;

        public Guid Guid => _mappedGuid;

        public GdFolderRef Empty => new GdFolderRef( 0, 0 );

        public GdFolderRef( UInt64 part1, UInt64 part2 )
        {
            _mappedGuid = Guid.Empty;
            guidPart1   = part1;
            guidPart2   = part2;
        }

        public override String ToString( )
        {
            return _mappedGuid.ToString();
        }

        public bool Equals(GdFolderRef other)
        {
            return _mappedGuid.Equals( other._mappedGuid );
        }

        public override bool Equals(object? obj)
        {
            return obj is GdFolderRef other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return _mappedGuid.GetHashCode();
        }

        public static bool operator ==(GdFolderRef left, GdFolderRef right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdFolderRef left, GdFolderRef right)
        {
            return !left.Equals( right );
        }

        public int CompareTo(GdFolderRef other)
        {
            return _mappedGuid.CompareTo( other._mappedGuid );
        }
    }
}
