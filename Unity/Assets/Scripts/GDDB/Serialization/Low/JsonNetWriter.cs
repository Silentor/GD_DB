using System;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
    public class JsonNetWriter : WriterBase
    {
        private readonly JsonWriter _writer;

        public JsonNetWriter( JsonWriter writer )
        {
            _writer = writer;
        }

        public override void WriteStartObject( )
        {
            _writer.WriteStartObject();
        }

        public override void WriteEndObject( )
        {
            _writer.WriteEndObject();
        }

        public override void WriteStartArray(EToken elementType )
        {
            _writer.WriteStartArray();
        }

        public override void WriteStartArray( )
        {
            _writer.WriteStartArray();
        }

        public override void WriteEndArray( )
        {
            _writer.WriteEndArray();
        }

        public override WriterBase WritePropertyName(String propertyName )
        {
            _writer.WritePropertyName( propertyName );
            return this;
        }

        public override void WriteNullValue( )
        {
            _writer.WriteNull();
        }

        public override void WriteValue( String value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue(Byte value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue(SByte value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue( Int32 value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue(Int64 value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue( UInt64 value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue(Single value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue(Double value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue(Boolean value )
        {
            _writer.WriteValue( value );
        }
    }
}