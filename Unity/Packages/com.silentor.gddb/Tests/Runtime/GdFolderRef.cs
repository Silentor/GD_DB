#nullable enable
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Gddb
{
    /// <summary>
    /// Reference to a gd folder. Autoresolved inside GDDB, but needs manually resolved if used outside.
    /// </summary>
    [Serializable]
    public struct GdFolderRef : IEquatable<GdFolderRef>, IComparable<GdFolderRef>
    {
        [SerializeField] UInt64 Part1;
        [SerializeField] UInt64 Part2;

        public Guid Guid => GuidToLongs.ToGuid( Part1, Part2 );

        public static GdFolderRef Empty => new GdFolderRef( 0, 0 );

        public GdFolderRef( UInt64 part1, UInt64 part2 )
        {
            Part1 = part1;
            Part2 = part2;
        }

        public override String ToString( )
        {
            return Guid.ToString();
        }

        public bool Equals(GdFolderRef other)
        {
            return Guid.Equals( other.Guid );
        }

        public override bool Equals(object? obj)
        {
            return obj is GdFolderRef other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return Guid.GetHashCode();
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
            return Guid.CompareTo( other.Guid );
        }
    }
}
