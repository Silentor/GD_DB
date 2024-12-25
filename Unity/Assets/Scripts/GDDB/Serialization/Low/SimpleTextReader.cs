using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace GDDB.Serialization
{
    /*
    public class SimpleTextReader : ReaderBase
    {
        private readonly TextReader    _reader;
        private readonly StringBuilder _buffer = new();
        private          String        _stringValue;
        private          Single        _singleValue;
        private          Double        _doubleValue;
        private          Int64         _int64Value;

        public SimpleTextReader( System.IO.TextReader reader )
        {
            _reader = reader;
        }

        public override EToken ReadNextToken( )
        {
            //return EToken.EoF;

            if( CurrentToken == EToken.EoF )
            {
                return EToken.EoF;
            }

            try
            {
                while ( true )
                {
                    if ( CurrentToken == EToken.BoF )
                    {
                        var chr = FindContainerStart(  );
                        if( chr == '{' )
                        {
                            CurrentToken = EToken.StartObject;
                            return EToken.StartObject;
                        }
                        else if( chr == '[' )
                        {
                            CurrentToken = EToken.StartArray;
                            return EToken.StartArray;
                        }
                        else
                            throw new Exception( $"Unexpected character at stream start {chr}" );
                    }

                    if ( CurrentToken == EToken.StartObject )
                    {
                        var nextTokenChar = FindPropertyStartOrObjectEnd(  );
                        if( nextTokenChar == '}' )
                        {
                            CurrentToken = EToken.EndObject;
                            return EToken.EndObject;
                        }
                        else if( nextTokenChar == '"' )
                        {
                            CurrentToken = EToken.PropertyName;
                            _stringValue = FindPropertyName(  );
                            return EToken.PropertyName;
                        }
                    } 

                    if( CurrentToken == EToken.PropertyName )
                    {
                        CurrentToken = FindValue();
                        return CurrentToken;
                    }
                }
            }
            catch ( EoFException e )
            {
                CurrentToken = EToken.EoF;
            }

            return EToken.EoF;
        }

        private EToken FindValue( )
        {
            while ( true )
            {
                var chr = ReadChar();
                if( chr == '"' )
                {
                    _buffer.Clear();

                    while ( true )
                    {
                        var chr2 = ReadChar();
                        if( chr2 == '"' )
                        {
                            _stringValue = _buffer.ToString();
                            return EToken.String;
                        }

                        _buffer.Append( chr2 );                        
                    }
                }
                else if( chr == '{' )
                {
                    return EToken.StartObject;
                }
                else if( chr == '[' )
                {
                    return EToken.StartArray;
                }
                else if( chr == '}' )
                {
                    return EToken.EndObject;
                }
                else if( chr == ']' )
                {
                    return EToken.EndArray;
                }
                else if( chr == 't' )
                {
                    ReadChar(); ReadChar(); ReadChar();
                    return EToken.True;
                }
                else if( chr == 'f' )
                {
                    ReadChar(); ReadChar(); ReadChar(); ReadChar();
                    return EToken.False;
                }
                else if( chr == 'n' )
                {
                    ReadChar(); ReadChar(); ReadChar();
                    return EToken.Null;
                }
                else if( chr == '-' || (chr >= '0' && chr <= '9') || chr == 'I' || chr == 'N' )
                {
                    _buffer.Clear();
                    _buffer.Append( chr );

                    while ( true )
                    {
                        var chr2 = ReadChar();
                        if( chr2 == ',' || chr2 == '}' || chr2 == ']' || chr2 == ' ' || chr2 == '\n' || chr2 == '\r' || chr2 == '\t' )
                        {
                            var numberBuffer = _buffer.ToString();
                            if ( numberBuffer.EndsWith( "f" ) )
                            {
                                _singleValue = Single.Parse( numberBuffer.AsSpan(0, numberBuffer.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture );
                                CurrentToken = EToken.Single;
                                return CurrentToken;
                            }
                            else if ( numberBuffer.EndsWith( "d" ) )
                            {
                                _doubleValue = Double.Parse( numberBuffer.AsSpan(0, numberBuffer.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture );
                                CurrentToken = EToken.Double;
                                return CurrentToken;
                            }
                            else if ( numberBuffer.EndsWith( "UL" ) )
                            {
                                _int64Value  = unchecked((Int64)UInt64.Parse( numberBuffer.AsSpan(0, numberBuffer.Length - 2), NumberStyles.Integer, CultureInfo.InvariantCulture ));
                                CurrentToken = EToken.UInt64;
                                return CurrentToken;
                            }
                            else
                            {
                                _int64Value  = Int32.Parse( numberBuffer, NumberStyles.Integer, CultureInfo.InvariantCulture );
                                CurrentToken = EToken.Int32;
                                return CurrentToken;
                            }
                        }

                        _buffer.Append( chr2 );                        
                    }
                }
                
            }
        }

        private Char FindContainerStart( )
        {
            while ( true )
            {
                var chr = ReadChar();
                if( chr == '{' || chr == '[' )
                {
                    return chr;
                }
            }    
        }

        private Char FindPropertyStartOrObjectEnd( )
        {
            while ( true )
            {
                var chr = ReadChar();
                if ( chr == '"' )
                    return '"';
                else if( chr == '}' )
                    return '}';
            }    
        }

        //We should already be on the first " character of the property name
        private String FindPropertyName( )
        {
            _buffer.Clear();

            while ( true )
            {
                var chr = ReadChar();
                if( chr == '"' )
                {
                    FindPropertyDelimiter();
                    return _buffer.ToString();
                }

                _buffer.Append( chr );                        
            }    
        }

        private void FindPropertyDelimiter( )
        {
            while ( ReadChar() != ':' )
            {
            }    
        }

        public override void ReadStartObject( )
        {
            ReadNextToken();
            EnsureStartObject();
        }

        public override void ReadEndObject( )
        {
            ReadNextToken();
            EnsureEndObject();
        }

        public override void ReadStartArray( )
        {
            ReadNextToken();
            EnsureToken( EToken.StartArray );
        }

        public override void ReadEndArray( )
        {
            ReadNextToken();
            EnsureEndArray();
        }

        public override void EnsureStartObject( )
        {
            EnsureToken( EToken.StartObject );
        }

        public override void EnsureEndObject( )
        {
            EnsureToken( EToken.EndObject );
        }

        public override void EnsureEndArray( )
        {
            EnsureToken( EToken.EndArray );
        }

        public override String GetPropertyName( )
        {
            EnsureToken( EToken.PropertyName );
            return _stringValue;
        }

        public override String GetStringValue( )
        {
            EnsureToken( EToken.String );
            return _stringValue;
        }

        public override Int32 GetInt32Value( )
        {
            if( CurrentToken == EToken.Int32 )
                return unchecked((Int32)_int64Value);
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.Int32} but got {CurrentToken}" );
        }

        public override UInt64 GetUInt64Value( )
        {
            if( CurrentToken == EToken.UInt64 )
                return unchecked((UInt64)_int64Value);
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.UInt64} but got {CurrentToken}" );
        }

        public override Single GetSingleValue( )
        {
            if( CurrentToken == EToken.Single )
                return _singleValue;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.Single} but got {CurrentToken}" );
        }

        public override Double GetDoubleValue( )
        {
            if( CurrentToken == EToken.Double )
                return _doubleValue;
            else if( CurrentToken == EToken.Null )
                return 0;
            else
                throw new Exception( $"Expected token {EToken.Double} but got {CurrentToken}" );
        }

        public override Boolean GetBoolValue( )
        {
            if( CurrentToken == EToken.True )
                return true;
            else if( CurrentToken == EToken.False || CurrentToken == EToken.Null )
                return false;
            else
                throw new Exception( $"Expected token {EToken.True} or {EToken.False} or {EToken.Null} but got {CurrentToken}" );
        }

        public override void SkipProperty( )
        {
            throw new NotImplementedException();
        }

        private Char ReadChar( )
        {
            var chrInt = _reader.Read();
            if( chrInt == -1 ) throw new EoFException(  );

            if ( chrInt == '\\' )
            {
                var escapedChar = _reader.Read();
                if( escapedChar == -1 ) throw new EoFException(  );
                switch ( escapedChar )
                {
                    case 'b':  return '\b';
                    case 'f':  return '\f';
                    case 'n':  return '\n';
                    case 'r':  return '\r';
                    case 't':  return '\t';
                    case '\\': return '\\';
                    case '"':  return '\"';
                    default:   throw new ArgumentOutOfRangeException( $"Unknown escape character {escapedChar}" );
                }
            }

            return (Char)chrInt;
        }

        private void EnsureToken( EToken token )
        {
            if ( CurrentToken == token )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected token {token} but got {CurrentToken}" );
        }



        private class EoFException : Exception
        {
            public EoFException( ) : base( "Unexpected end of file" )
            {
            }
        }
    }
    */
}