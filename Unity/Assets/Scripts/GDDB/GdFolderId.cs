using System;
using System.Runtime.InteropServices;

namespace GDDB
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct GdFolderId : IEquatable<GdFolderId>
    {
        [FieldOffset(0)]
        public Guid   GUID;
        [FieldOffset(0)]
        public UInt64 Serializalble1;
        [FieldOffset(8)]
        public UInt64 Serializalble2;

        public GdFolderId( UInt64 part1, UInt64 part2 )
        {
            GUID           = default;
            Serializalble1 = part1;
            Serializalble2 = part2;
        }

        public override String ToString( )
        {
            return GUID.ToString();
        }

        public bool Equals(GdFolderId other)
        {
            return GUID.Equals( other.GUID );
        }

        public override bool Equals(object? obj)
        {
            return obj is GdFolderId other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return GUID.GetHashCode();
        }

        public static bool operator ==(GdFolderId left, GdFolderId right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdFolderId left, GdFolderId right)
        {
            return !left.Equals( right );
        }
    }
}
