using System;

namespace GDDB
{
    [Serializable]
    public struct GdType : IEquatable<GdType>
    {
        public Byte Cat1;
        public Byte Cat2;
        public Byte Cat3;
        public Byte Element;

        public static readonly GdType None = default;

        public bool Equals(GdType other)
        {
            return Cat1 == other.Cat1 && Cat2 == other.Cat2 && Cat3 == other.Cat3 && Element == other.Element;
        }

        public override bool Equals(object obj)
        {
            return obj is GdType other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return HashCode.Combine( Cat1, Cat2, Cat3, Element );
        }

        public static bool operator ==(GdType left, GdType right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdType left, GdType right)
        {
            return !left.Equals( right );
        }
    }
}