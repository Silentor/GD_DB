using System;

namespace Gddb.Serialization
{
    public enum EToken : Byte
    {
        //Control
        BoF = 0,
        EoF = 1,

        DataToken = 1 << 2,                            //Value stored in token itself
        Null      = DataToken,
        False     = DataToken + 1,
        True      = DataToken + 2, 

        Extensions  = 1 << 3,
        //Type        = Extensions,

        String       = 1 << 4,
        PropertyName = String + 1,

        Container    = 1 << 5,
        StartObject  = Container,
        EndObject    = Container + 1,
        StartArray   = Container + 2,
        EndArray     = Container + 3,
        StartBuffer  = Container + 4,
        EndBuffer    = Container + 5,

        Number  = 1 << 6,
        Int8     = Number,
        UInt8    = Number + 1,
        Int16    = Number + 2,
        UInt16   = Number + 3,
        Int32    = Number + 4,
        UInt32   = Number + 5,
        Int64    = Number + 6,
        UInt64   = Number + 7,
        //Int128   = Integer + 8,
        //UInt128  = Integer + 9,
        VarInt   = Number + 10,
        Guid     = Number + 11,
        Single   = Number + 12,
        Double   = Number + 13,
        //Decimal  = Number + 14,
        //DateTime  = Number + 15,

        // Enum    = Number + (1 << 4),
        // Enum1   = Enum,
        // Enum2   = Enum + 1,
        // Enum4   = Enum + 2,
        // Enum8   = Enum + 3,

        Alias   = 1 << 7,
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
            return token >= EToken.Single && token <= EToken.Double;
        }

        public static Boolean IsStringToken( this EToken token )
        {
            return ((Byte)token & 0b1111_0000) == (Byte)EToken.String;
        }

        public static Boolean IsContainerToken( this EToken token )
        {
            return ((Byte)token & 0b1110_0000) == (Byte)EToken.Container;
        }

        public static Boolean IsNumberToken( this EToken token )
        {
            return ((Byte)token & 0b1100_0000) == (Byte)EToken.Number;
        }

        public static Boolean IsIntegerToken( this EToken token )
        {
            return token >= EToken.Int8 && token <= EToken.UInt64;
        }

        public static Boolean IsBooleanToken( this EToken token )
        {
            return token == EToken.True || token == EToken.False;
        }

        // public static Boolean IsEnumToken( this EToken token )
        // {
        //     return ((Byte)token & 0b1101_0000) == (Byte)EToken.Enum;
        // }

        public static Boolean IsAliasToken( this EToken token )
        {
            return ((Byte)token & (Byte)EToken.Alias) == (Byte)EToken.Alias;
        }

        public static Boolean HasPayload( this EToken token )
        {
            return token.IsStringToken() || token.IsNumberToken();
        }

    }
}