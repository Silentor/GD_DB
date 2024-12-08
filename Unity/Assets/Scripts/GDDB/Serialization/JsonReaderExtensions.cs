using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
    public static class JsonReaderExtensions
    {
        public static void EnsureProperty( this JsonReader reader, String propertyName )
        {
            if ( reader.TokenType == JsonToken.PropertyName && String.Equals( reader.Value, propertyName ) )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected property {propertyName} but got token {reader.TokenType}, value {reader.Value}" );
        }
        public static void EnsureNextProperty( this JsonReader reader, String propertyName )
        {
            if ( reader.Read() )
            {
                EnsureProperty( reader, propertyName );
            }
            else
                throw new Exception( $"Unexpected end of file" );
        }
        public static void EnsureToken( this JsonReader reader, JsonToken tokenType )
        {
            if ( reader.TokenType == tokenType )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected token {tokenType} but got {reader.TokenType} = {reader.Value}" );
        }
        
        public static void EnsureNextToken( this JsonReader reader, JsonToken tokenType )
        {
            if ( reader.Read() )
            {
                EnsureToken( reader, tokenType );
            }
            else
                throw new Exception( $"Unexpected end of file" );
        }
        
        public static String ReadPropertyName( this JsonReader reader )
        {
            EnsureNextToken( reader, JsonToken.PropertyName );
            return (String)reader.Value;
        }

        public static String ReadPropertyString( this JsonReader reader, String propertyName, Boolean alreadyOnStart )
        {
            if( alreadyOnStart )
                EnsureToken( reader, JsonToken.PropertyName );
            else
                EnsureNextToken( reader, JsonToken.PropertyName );
            if ( String.Equals(reader.Value, propertyName )) 
            {
                return reader.ReadAsString();
            }
            else
                throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        }

        public static Int64 ReadPropertyLong( this JsonReader reader, String propertyName, Boolean alreadyOnStart )
        {
            if( alreadyOnStart )
                EnsureToken( reader, JsonToken.PropertyName );
            else
                EnsureNextToken( reader, JsonToken.PropertyName );
            if ( String.Equals(reader.Value, propertyName )) 
            {
                EnsureNextToken( reader, JsonToken.Integer );
                return (Int64)reader.Value;
            }
            else
                throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        }
        public static Int32 ReadPropertyInt( this JsonReader reader, String propertyName, Boolean alreadyOnStart )
        {
            if( alreadyOnStart )
                EnsureToken( reader, JsonToken.PropertyName );
            else
                EnsureNextToken( reader, JsonToken.PropertyName );
            if ( String.Equals(reader.Value, propertyName ))
            {
                return reader.ReadAsInt32().Value;
            }
            else
                throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        }
    }
}