namespace GDDB.Serialization
{
    public enum EToken : byte
    {
        //Control
        BoF = 0,
        EoF = 1,

        Boolean = 1 << 2,
        False   = Boolean,
        True    = Boolean + 1,

        Float  = 1 << 3,
        Single = Float,
        Double = Float + 1,

        String              = 1 << 4,

        Container    = 1 << 5,
        StartObject  = Container,
        EndObject    = Container + 1,
        StartArray   = Container + 2,
        EndArray     = Container + 3,
        StartBuffer  = Container + 4,
        EndBuffer    = Container + 5,
        PropertyName = Container + 6,

        Integer = 1 << 6,
        Int32   = Integer,
        Int64   = Integer + 1,
        UInt32  = Integer + 2,
        UInt64  = Integer + 3,
        Byte    = Integer + 4,
        SByte   = Integer + 5,
        Int16   = Integer + 6,
        UInt16  = Integer + 7,

        Alias               = 1 << 7,
    }
}