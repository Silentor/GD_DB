using System;
using System.IO;

namespace GDDB.Serialization
{
    public class BinaryWriter : WriterBase
    {
        public override String Path => "Not implemented";

        private readonly System.IO.BinaryWriter _writer;

        public BinaryWriter( Stream writer )
        {
            _writer = new System.IO.BinaryWriter( writer );
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

        public override WriterBase WritePropertyName(String propertyName )
        {
            _writer.Write( (byte)EToken.PropertyName );
            _writer.Write( propertyName );
            return this;
        }

        public override void WriteNullValue( )
        {
            _writer.Write( (byte)EToken.Null );
        }

        public override void WriteValue(String value )
        {
            if( value != null )
            {
                _writer.Write( (byte)EToken.String );
                _writer.Write( value );
            }
            else
            {
                _writer.Write( (byte)EToken.Null );
            }
        }

        public override void WriteValue(Byte value )
        {
            _writer.Write( (byte)EToken.UInt8 );
            _writer.Write( value );
        }

        public override void WriteValue(SByte value )
        {
            _writer.Write( (byte)EToken.Int8 );
            _writer.Write( value );
        }

        public override void WriteValue(Int32 value )
        {
            _writer.Write( (byte)EToken.Int32 );
            _writer.Write( value );
        }

        public override void WriteValue(Int64 value )
        {
            _writer.Write( (byte)EToken.Int64 );
            _writer.Write( value );
        }

        public override void WriteValue(UInt64 value )
        {
            _writer.Write( (byte)EToken.UInt64 );
            _writer.Write( value );
        }

        public override void WriteValue(Single value )
        {
            _writer.Write( (byte)EToken.Single );
            _writer.Write( value );
        }

        public override void WriteValue(Double value )
        {
            _writer.Write( (byte)EToken.Double );
            _writer.Write( value );

        }

        public override void WriteValue(Boolean value )
        {
            if( value )
                _writer.Write( (byte)EToken.True );
            else
                _writer.Write( (byte)EToken.False );
        }

        public override void WriteValue(Guid value )
        {
            if ( value != Guid.Empty )
            {
                _writer.Write( (byte)EToken.Guid );
                Span<Byte> bytes = stackalloc Byte[16];
                if( value.TryWriteBytes( bytes ) )
                    _writer.Write( bytes );
            }
            else
            {
                _writer.Write( (byte)EToken.Null );
            }
        }

        public override void WriteValue(Enum value )
        {
            var underlyingType = Enum.GetUnderlyingType( value.GetType() );
            if ( underlyingType == typeof(Byte) )
            {
                _writer.Write( (byte)EToken.Enum1 );
                _writer.Write( Convert.ToByte( value ) );
            }
            else if( underlyingType == typeof(SByte) )
            {
                _writer.Write( (byte)EToken.Enum1 );
                _writer.Write( Convert.ToSByte( value ) );
            }
            else if( underlyingType == typeof(Int16) )
            {
                _writer.Write( (byte)EToken.Enum2 );
                _writer.Write( Convert.ToInt16( value ) );
            }
            else if( underlyingType == typeof(UInt16) )
            {
                _writer.Write( (byte)EToken.Enum2 );
                _writer.Write( Convert.ToUInt16( value ) );
            }
            else if( underlyingType == typeof(Int32) )
            {
                _writer.Write( (byte)EToken.Enum4 );
                _writer.Write( Convert.ToInt32( value ) );
            }
            else if( underlyingType == typeof(UInt32) )
            {
                _writer.Write( (byte)EToken.Enum4 );
                _writer.Write( Convert.ToUInt32( value ) );
            }
            else if( underlyingType == typeof(Int64) )
            {
                _writer.Write( (byte)EToken.Enum8 );
                _writer.Write( Convert.ToInt64( value ) );
            }
            else if( underlyingType == typeof(UInt64) )
            {
                _writer.Write( (byte)EToken.Enum8 );
                _writer.Write( Convert.ToUInt64( value ) );
            }
            else
                throw new Exception( $"Unsupported enum type {underlyingType}" );

        }
    }
}