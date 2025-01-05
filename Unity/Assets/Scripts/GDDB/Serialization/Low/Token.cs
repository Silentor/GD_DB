using System;

namespace GDDB.Serialization
{
    public enum EToken : Byte
    {
        //Control
        BoF = 0,
        EoF = 1,

        DataToken = 1 << 2,
        Null      = DataToken,
        False     = DataToken + 1,
        True      = DataToken + 2, 

        Float  = 1 << 3,
        Single = Float,
        Double = Float + 1,

        String       = 1 << 4,
        PropertyName = String + 1,

        Container    = 1 << 5,
        StartObject  = Container,
        EndObject    = Container + 1,
        StartArray   = Container + 2,
        EndArray     = Container + 3,
        StartBuffer  = Container + 4,
        EndBuffer    = Container + 5,

        Integer = 1 << 6,
        Int8    = Integer,
        UInt8   = Integer + 1,
        Int16   = Integer + 2,
        UInt16  = Integer + 3,
        Int32   = Integer + 4,
        UInt32  = Integer + 5,
        Int64   = Integer + 6,
        UInt64  = Integer + 7,
        VarInt  = Integer + 8,
        Guid    = Integer + 9,

        Enum1   = Integer + (1 << 4),
        Enum2   = Enum1 + 1,
        Enum4   = Enum1 + 2,
        Enum8   = Enum1 + 3,

        Alias               = 1 << 7,
    }

    public static class TokenExtensions
    {
        public static Boolean IsStartContainer( this EToken token )
        {
            return token == EToken.StartObject || token == EToken.StartArray || token == EToken.StartBuffer;
        }

        public static Boolean IsEndContainer( this EToken token )
        {
            return token == EToken.EndObject || token == EToken.EndArray || token == EToken.EndBuffer;
        }

        public static Boolean IsDataToken( this EToken token )
        {
            return ((Byte)token & 0b1111_1100) == (Byte)EToken.DataToken;
        }

        public static Boolean IsFloatToken( this EToken token )
        {
            return ((Byte)token & 0b1111_1000) == (Byte)EToken.Float;
        }

        public static Boolean IsStringToken( this EToken token )
        {
            return ((Byte)token & 0b1111_0000) == (Byte)EToken.String;
        }

        public static Boolean IsContainerToken( this EToken token )
        {
            return ((Byte)token & 0b1110_0000) == (Byte)EToken.Container;
        }

        public static Boolean IsIntegerToken( this EToken token )
        {
            return ((Byte)token & 0b1100_0000) == (Byte)EToken.Integer;
        }

        public static Boolean IsBooleanToken( this EToken token )
        {
            return token == EToken.True || token == EToken.False;
        }

        public static Boolean IsEnumToken( this EToken token )
        {
            return ((Byte)token & 0b1101_0000) == (Byte)EToken.Enum1;
        }

        public static Boolean HasPayload( this EToken token )
        {
            return token.IsFloatToken() || token.IsStringToken() || token.IsIntegerToken();
        }

    }
}