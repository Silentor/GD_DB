using System;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
       public abstract class WriterBase
    {
        public abstract void WriteStartObject( );

        public abstract void WriteEndObject( );
        public abstract void WriteStartArray( EToken elementType );
        public abstract void WriteStartArray( );
        public abstract void WriteEndArray( );

        public abstract void WritePropertyName( string propertyName );

        public abstract void WriteValue( string value );
        public abstract void WriteValue( Int32 value );
    }

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

        public override void WritePropertyName(String propertyName )
        {
            _writer.WritePropertyName( propertyName );
        }

        public override void WriteValue( String value )
        {
            _writer.WriteValue( value );
        }

        public override void WriteValue( Int32 value )
        {
            _writer.WriteValue( value );
        }
    }

    public class BinaryWriter : WriterBase
    {
        private readonly System.IO.BinaryWriter _writer;

        public BinaryWriter( System.IO.BinaryWriter writer )
        {
            _writer = writer;
        }

        public override void WriteStartObject( )
        {
            _writer.Write( (byte)EToken.StartObject );
        }

        public override void WriteEndObject( )
        {
            _writer.Write( (byte)EToken.EndObject );
        }

        public override void WriteStartArray( EToken elementType )
        {
            _writer.Write( (byte)EToken.StartBuffer );
        }

        public override void WriteStartArray( )
        {
            _writer.Write( (byte)EToken.StartArray );
        }

        public override void WriteEndArray( )
        {
            _writer.Write( (byte)EToken.EndArray );
        }

        public override void WritePropertyName(String propertyName )
        {
            _writer.Write( (byte)EToken.PropertyName );
            _writer.Write( propertyName );
        }

        public override void WriteValue(String value )
        {
            _writer.Write( (byte)EToken.String );
            _writer.Write( value );
        }

        public override void WriteValue(Int32 value )
        {
            _writer.Write( (byte)EToken.Int32 );
            _writer.Write( value );
        }
    }
}
