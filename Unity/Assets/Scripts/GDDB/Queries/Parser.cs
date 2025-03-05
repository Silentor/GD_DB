using System;
using System.Collections.Generic;

namespace GDDB.Queries
{
    /// <summary>
    /// Parses glob-like path and string queries to tokens. * - zero or more symbols, ? - exactly one symbol, ** - current folder and all subfolders recursively
    /// </summary>
    /// <example>
    /// * - all files in current (root) folder
    /// mydata - file with name mydata in current (root) folder
    /// */* - all files in all subfolders of current (root) folder
    /// **/* - all files in all subfolders of current (root) folder and in all subfolders of subfolders. Shortly all files in DB
    /// Mobs/*skin - all files in Mobs folder with names ending with skin
    /// Mobs/Orcs/orc_?? - all files kind of orc_01, orc_02, etc in Orcs folder of Mobs folder
    /// </example>
    public class Parser
    {
        private readonly Executor             _executor;
        private readonly List<HierarchyToken> _buffer = new (  );

        public Parser( Executor executor )
        {
            _executor = executor;
        }

        public HierarchyToken ParseObjectsQuery( String query )
        {
            if ( String.IsNullOrEmpty( query ) )
                return null;
        
            var parts = query.Split( '/' );
            _buffer.Clear();
            for ( int i = 0; i < parts.Length; i++ )
            {
                if( i < parts.Length - 1 )
                    _buffer.Add( ParseFolderToken( parts[ i ] ) );
                else
                    _buffer.Add( ParseObjectsToken( parts[ i ] ) );                
            }

            //Optimize "**/*" query (all objects in db)
            if ( _buffer.Count == 2 && _buffer[ 0 ] is AllSubfoldersRecursivelyToken &&  _buffer[ 1 ] is AllFilesToken )              
            {
                _buffer.Clear();
                _buffer.Add( new AllFilesInDBToken( _executor.DB ) );
            }

            //Special case: If no path specified, search entire DB (like Unity search bar). If no wildcard, convert it to Contains wildcard
            if( _buffer.Count == 1 && _buffer[0] is WildcardFilesToken onlyObjName )  
            {
                _buffer.Clear();
                _buffer.Add( new AllFoldersInDBToken( _executor.DB ) );
                if( onlyObjName.Wildcard is LiteralToken nameLiteral && nameLiteral.NextToken == null )
                    _buffer.Add( new WildcardFilesToken( new ContainsLiteralToken( nameLiteral.Literal ), _executor ) );
                else
                    _buffer.Add( onlyObjName );
            }

            //Root folder token can be safely deleted
            if( _buffer.Count > 0 && _buffer[0] is IdentityFolderToken )
                _buffer.RemoveAt( 0 );

            //Link syntax tree
            for ( int i = 0; i < _buffer.Count - 1; i++ )
            {
                if ( _buffer[ i ] is FolderToken fToken )
                {
                    fToken.NextToken = _buffer[ i + 1 ];
                }
            }

            return _buffer[ 0 ];
        }

        public HierarchyToken ParseFoldersQuery( String query )
        {
            if ( String.IsNullOrEmpty( query ) )
                return null;
        
            var parts  = query.Split( '/' );
            _buffer.Clear();
            for ( int i = 0; i < parts.Length; i++ )
            {
                _buffer.Add( ParseFolderToken( parts[ i ] ) );
            }
    
            //Optimizing syntax "tree"
            //Special case: If no path specified, search entire DB (like Unity search bar). If no wildcard, convert it to Contains wildcard
            if( _buffer.Count == 1 && _buffer[0] is WildcardSubfoldersToken onlyFolderName )  
            {
                _buffer.Clear();
                _buffer.Add( new AllFoldersInDBToken( _executor.DB ) );
                if( onlyFolderName.Wildcard is LiteralToken nameLiteral && nameLiteral.NextToken == null )
                    _buffer.Add( new WildcardSubfoldersToken( new ContainsLiteralToken( nameLiteral.Literal ), _executor ) );
                else
                    _buffer.Add( onlyFolderName );
            }

            //Root folder token can be safely deleted
            if( _buffer.Count > 0 && _buffer[0] is IdentityFolderToken )
                _buffer.RemoveAt( 0 );

            //Link syntax tree
            for ( int i = 0; i < _buffer.Count - 1; i++ )
            {
                if ( _buffer[ i ] is FolderToken fToken )
                {
                    fToken.NextToken = _buffer[ i + 1 ];
                }
            }

            return _buffer[ 0 ];
        }

        public StringToken ParseString( String value )
        {
            //Fast pass
            if ( value == "*" )
                return new AnyTextToken();

            StringToken prevToken = null;
            StringToken result    = null;
            var         position  = 0;
            var count = 0;
            while ( position < value.Length )
            {
                var token = ExtractNextStringToken( value, ref position );
                if ( result == null )
                    result = token;
                if ( prevToken != null )
                    prevToken.NextToken = token;
                prevToken = token;
                count++;
            }

            //Optimization pass for common queries
            if ( count == 2 )
            {
                if ( result is AnyTextToken && result.NextToken is LiteralToken lt )
                {
                    return new AsterixAndLiteralToken( lt.Literal );
                }
                else if( result is LiteralToken lt2 && result.NextToken is AnyTextToken )
                {
                    return new LiteralAndAsterixToken( lt2.Literal );
                }
            }
            else if ( count == 3 )
            {
                if( result is AnyTextToken && result.NextToken is LiteralToken lt && lt.NextToken is AnyTextToken )
                {
                    return new ContainsLiteralToken( lt.Literal );
                }
                else if( result is LiteralToken lt2 && lt2.NextToken is AnyTextToken && lt2.NextToken.NextToken is LiteralToken lt3 )
                {
                    return new AsterixBetweenLiteralsToken( lt2.Literal, lt3.Literal );
                }
            }

            return result;
        }
        
        private FolderToken ParseFolderToken( String queryPart )
        {
            if ( queryPart == String.Empty )
                return new IdentityFolderToken();
            else if( queryPart == "*" )
                return new AllSubfoldersToken();
            else if ( queryPart == "**" )
                return new AllSubfoldersRecursivelyToken();
            else
            {
                return new WildcardSubfoldersToken( ParseString( queryPart ), _executor );
            }
        }

        private FileToken ParseObjectsToken( String queryPart )
        {
            if( queryPart == "*" )
                return new AllFilesToken();
            else
            {
                return new WildcardFilesToken( ParseString( queryPart ), _executor );
            }
        }

        

        private StringToken ExtractNextStringToken( String value, ref Int32 position )
        {          
            if ( value[ position ] == '*' )
            {
                for ( int i = position + 1; i < value.Length; i++ )     //Consume all asterixes and return AnyTextToken
                {
                    if ( value[ i ] != '*' )
                    {
                        position = i;
                        return new AnyTextToken();
                    }    
                }
                position = value.Length;
                return new AnyTextToken();
            }
            else if ( value[ position ] == '?' )
            {
                for ( int i = position + 1; i < value.Length; i++ )     //Consume all ? and return SomeSymbolToken with counter
                {
                    if ( value[ i ] != '?' )
                    {
                        var symbolsCount = i - position;
                        position = i;
                        return new SomeSymbolToken( symbolsCount );
                    }    
                }
                var count = value.Length - position;
                position = value.Length;
                return new SomeSymbolToken( count );
            }
            else                                                        //Extract string literal
            {
                for ( int i = position + 1; i < value.Length; i++ )
                {
                    if ( value[i] == '*' || value[i] == '?' )
                    {
                        var literal = value.Substring( position, i - position );
                        position = i;
                        return new LiteralToken( literal );
                    }    
                }

                var literal2 = value.Substring( position );
                position = value.Length;
                return new LiteralToken( literal2 );
            }
        }

        private Boolean IsAnyWildcards( String value )
        {
            foreach ( var c in value )
            {
                if ( c == '*' || c == '?' )
                    return true;
            }

            return false;
        }
    }
}