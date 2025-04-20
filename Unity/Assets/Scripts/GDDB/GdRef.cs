#nullable enable
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Gddb
{
    [Serializable]
    public struct GdRef : IEquatable<GdRef>, IComparable<GdRef>
    {
        [SerializeField] UInt64 Part1;
        [SerializeField] UInt64 Part2;

        public Guid Guid => GuidToLongs.ToGuid( Part1, Part2 );

        public GdRef( UInt64 part1, UInt64 part2 )
        {
            Part1 = part1;
            Part2 = part2;
        }

        public GdRef( Guid guid )
        {
            (Part1, Part2) = GuidToLongs.ToLongs( guid );
        }

        public override String ToString( )
        {
            return Guid.ToString();
        }

        public bool Equals(GdRef other)
        {
            return Guid.Equals( other.Guid );
        }

        public override bool Equals(object? obj)
        {
            return obj is GdRef other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return Guid.GetHashCode();
        }

        public static bool operator ==(GdRef left, GdRef right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdRef left, GdRef right)
        {
            return !left.Equals( right );
        }

        public int CompareTo(GdRef other)
        {
            return Guid.CompareTo( other.Guid );  
        }

    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public ref struct GuidToLongs
    {
        [FieldOffset(0)]
        public Guid   GUID;
        [FieldOffset(0)]
        public UInt64 ULong1;
        [FieldOffset(8)]
        public UInt64 ULong2;

        public static Guid ToGuid( UInt64 part1, UInt64 part2 )
        {
            var guidToLongs = new GuidToLongs { ULong1 = part1, ULong2 = part2 };
            return guidToLongs.GUID;
        }

        public static (UInt64 part1, UInt64 part2) ToLongs( Guid guid )
        {
            var guidToLongs = new GuidToLongs { GUID = guid };
            return (guidToLongs.ULong1, guidToLongs.ULong2);
        }
    }
}
