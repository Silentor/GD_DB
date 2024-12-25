using System;
using System.Collections.Generic;
using System.Globalization;

namespace GDDB.Serialization
{
    /*
    public class SimpleTextWriter : WriterBase
    {
        private readonly System.IO.TextWriter _writer;
        private readonly List<ContainerState> _containers = new ();

        public SimpleTextWriter( System.IO.TextWriter writer )
        {
            _writer = writer;
        }

        public override void WriteStartObject( )
        {
            PrependCommaForArrayElement();

            _writer.Write( "{ " );
            _containers.Add( new ContainerState { Container = EToken.StartObject, ElementCount = 0 } );
        }

        public override void WriteEndObject( )
        {
            _writer.Write( " }" );
            _containers.RemoveAt( _containers.Count - 1 );
        }

        public override void WriteStartArray( EToken elementType )
        {
            PrependCommaForArrayElement();

            _writer.Write( "[ " );
            _containers.Add( new ContainerState { Container = EToken.StartArray, ElementCount = 0 } );
        }

        public override void WriteStartArray( )
        {
            PrependCommaForArrayElement();

            _writer.Write( "[ " );
            _containers.Add( new ContainerState { Container = EToken.StartArray, ElementCount = 0 } );
        }

        public override void WriteEndArray( )
        {
            _writer.Write( " ]" );
            _containers.RemoveAt( _containers.Count - 1 );
        }

        public override void WritePropertyName(String propertyName )
        {
            PrependCommaForObjectProperty();

            _writer.Write( "\"" );
            _writer.Write( propertyName );
            _writer.Write( "\": " );
        }
        
        public override void WriteNullValue( )
        {
            PrependCommaForArrayElement();

            _writer.Write( "null" );
        }

        public override void WriteValue(String value )
        {
            PrependCommaForArrayElement();

            _writer.Write( "\"" );
            _writer.Write( value );
            _writer.Write( "\"" );
        }

        public override void WriteValue(Int32 value )
        {
            PrependCommaForArrayElement();

            _writer.Write( value );
        }

        public override void WriteValue(UInt64 value )
        {
            PrependCommaForArrayElement();

            _writer.Write( value );
            _writer.Write( "UL" );
        }

        public override void WriteValue(Single value )
        {
            PrependCommaForArrayElement();

            _writer.Write( value.ToString("R", CultureInfo.InvariantCulture) );
            _writer.Write( "f" );
        }

        public override void WriteValue(Double value )
        {
            PrependCommaForArrayElement();

            _writer.Write( value.ToString("R", CultureInfo.InvariantCulture) );
            _writer.Write( "d" );
        }

        public override void WriteValue(Boolean value )
        {
            PrependCommaForArrayElement();

            if( value )
                _writer.Write( "true" );
            else
                _writer.Write( "false" );
        }

        private void PrependCommaForArrayElement( )
        {
            if( _containers.Count == 0 )
                return;

            var currentContainer = _containers[^1];
            if ( currentContainer.Container == EToken.StartArray  )
            {
                if( currentContainer.ElementCount > 0 )
                    _writer.Write( ", " );
                currentContainer.ElementCount++;
                _containers[^1] = currentContainer;
            }
        }

        private void PrependCommaForObjectProperty( )
        {
            var currentContainer = _containers[^1];
            if ( currentContainer.Container == EToken.StartObject  )
            {
                if( currentContainer.ElementCount > 0 )
                    _writer.Write( ", " );
                currentContainer.ElementCount++;
                _containers[^1] = currentContainer;
            }
        }

        private void EscapeString( String value )
        {
            foreach( var c in value )
            {
                switch( c )
                {
                    case '\b':
                        _writer.Write( "\\b" );
                        break;
                    case '\f':
                        _writer.Write( "\\f" );
                        break;
                    case '\n':
                        _writer.Write( "\\n" );
                        break;
                    case '\r':
                        _writer.Write( "\\r" );
                        break;
                    case '\t':
                        _writer.Write( "\\t" );
                        break;
                    case '\\':
                        _writer.Write( "\\\\" );
                        break;
                    case '\"':
                        _writer.Write( "\\\"" );
                        break;
                    default:
                        _writer.Write( c );
                        break;
                }
            }
        }

        private struct ContainerState
        {
            public EToken Container;
            public Int32  ElementCount;
        }
    }
    */
}