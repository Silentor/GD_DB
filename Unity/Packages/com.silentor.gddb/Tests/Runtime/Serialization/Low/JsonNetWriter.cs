using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace Gddb.Serialization
{
    public class JsonNetWriter : WriterBase
    {
        private readonly JsonWriter _writer;

        public override String Path => _writer.Path;

        public JsonNetWriter( StringBuilder stringBuilder, Boolean indent )
        {
            var jsonTextWriter = new JsonTextWriter( new System.IO.StringWriter( stringBuilder, CultureInfo.InvariantCulture ) );
            if( indent )
            {
                jsonTextWriter.Formatting = Formatting.Indented;
                jsonTextWriter.Indentation = 4;
            }
            else
            {
                jsonTextWriter.Formatting = Formatting.None;
            }

            _writer                    = jsonTextWriter;
        }

        public JsonNetWriter( System.IO.Stream stream, Boolean indent )
        {
            var jsonTextWriter = new JsonTextWriter( new System.IO.StreamWriter( stream ) );
            if( indent )
            {
                jsonTextWriter.Formatting  = Formatting.Indented;
                jsonTextWriter.Indentation = 4;
            }
            else
            {
                jsonTextWriter.Formatting = Formatting.None;
            }
            _writer                    = jsonTextWriter;
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

        public override void WriteValue(Guid value )
        {
            _writer.WriteValue( value.ToString("D", CultureInfo.InvariantCulture) );
        }

        public override void WriteValue(Enum value )
        {
            _writer.WriteValue( value.ToString( "G" ) );
        }

        public override void WriteValue( Type value )
        {
            var result = $"{value.FullName}, {value.Assembly.GetName().Name}";
            _writer.WriteValue( result );
        }

        protected override void WriteRaw(Byte value )
        {
            //Its a binary stuff
        }
    }
}