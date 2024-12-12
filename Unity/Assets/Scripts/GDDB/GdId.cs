#nullable enable
using System;
using System.Runtime.InteropServices;

namespace GDDB
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct GdId : IEquatable<GdId>
    {
        [FieldOffset(0)]
        public Guid   GUID;
        [FieldOffset(0)]
        public UInt64 Serializalble1;
        [FieldOffset(8)]
        public UInt64 Serializalble2;

        public GdId( UInt64 part1, UInt64 part2 )
        {
            GUID           = default;
            Serializalble1 = part1;
            Serializalble2 = part2;
        }

        public override String ToString( )
        {
            return GUID.ToString();
        }

        public bool Equals(GdId other)
        {
            return GUID.Equals( other.GUID );
        }

        public override bool Equals(object? obj)
        {
            return obj is GdId other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return GUID.GetHashCode();
        }

        public static bool operator ==(GdId left, GdId right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdId left, GdId right)
        {
            return !left.Equals( right );
        }
    }
}
