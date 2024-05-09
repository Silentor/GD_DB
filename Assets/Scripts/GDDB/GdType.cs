using System;
using System.Runtime.InteropServices;

namespace GDDB
{
    [Serializable]
    public struct GdType : IEquatable<GdType>
    {
        public UInt32 Data;

        public Int32 this[ Int32 categoryIndex ]
        {
            get
            {
                CheckIndex( categoryIndex );
                return (Int32)((Data >> ((3 - categoryIndex) * 8)) & 0xFF);
            }
            set
            {
                CheckIndex( categoryIndex );
                categoryIndex = 3 - categoryIndex;
                Data  &= ~(UInt32)(0xFF << (categoryIndex * 8));
                Data  |= (UInt32)((value & 0xFF) << (categoryIndex * 8));
            }
        }


        public static readonly GdType None = default;

        public GdType( UInt32 rawData )
        {
            Data = rawData;
        }

        public GdType( Int32 rawData )
        {
            Data = (UInt32)rawData;
        }

        public GdType WithCategory( Int32 categoryIndex, Int32 value )
        {
            CheckIndex( categoryIndex );
            var copy = this;
            copy[ categoryIndex ] = value;
            return copy;
        }

        public override String ToString( )
        {
            return $"{this[ 0 ]}.{this[ 1 ]}.{this[ 2 ]}.{this[ 3 ]}";
        }

        public bool Equals(GdType other)
        {
            return Data == other.Data;
        }

        public override bool Equals(object obj)
        {
            return obj is GdType other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return (Int32)Data;
        }

        public static bool operator ==(GdType left, GdType right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdType left, GdType right)
        {
            return !left.Equals( right );
        }

        private void CheckIndex( Int32 index )
        {
            if ( index < 0 || index > 3 )
            {
                throw new IndexOutOfRangeException( "Index must be between 0 and 3" );
            }
        }
    }
}