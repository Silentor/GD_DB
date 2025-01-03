using System;

namespace GDDB.Serialization
{
       public abstract class WriterBase
    {
        public abstract String Path { get; }

        public abstract void WriteStartObject( );

        public abstract void WriteEndObject( );
        public abstract void WriteStartArray( EToken elementType );
        public abstract void WriteStartArray( );
        public abstract void WriteEndArray( );

        public abstract WriterBase WritePropertyName( String propertyName );

        public abstract void WriteNullValue(  );
        public abstract void WriteValue( String value );
        public abstract void WriteValue( Byte value );
        public abstract void WriteValue( SByte value );
        public abstract void WriteValue( Int32 value );
        public abstract void WriteValue( Int64 value );
        public abstract void WriteValue( UInt64 value );
        public abstract void WriteValue( Single value );
        public abstract void WriteValue( Double value );
        public abstract void WriteValue( Boolean value );
        public abstract void WriteValue( Guid value );
    }
}
