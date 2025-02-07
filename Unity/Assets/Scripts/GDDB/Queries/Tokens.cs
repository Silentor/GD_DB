using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB.Queries
{
    public abstract class HierarchyToken
    {
        public static FolderToken Append( params HierarchyToken[] tokens )
        {
            for ( int i = 0; i < tokens.Length; i++ )
            {
                if( tokens[ i ] is FolderToken fToken )                    
                    fToken.NextToken = i < tokens.Length - 1 ? tokens[ i + 1 ] : null;
            }

            return (FolderToken)tokens[0];
        }
    }

    public abstract class FileToken : HierarchyToken
    {
        public abstract void ProcessFolder( IReadOnlyList<GdFolder> input, List<ScriptableObject> output );
    }

    public abstract class FolderToken : HierarchyToken
    {
        public HierarchyToken NextToken;

        public abstract void ProcessFolder( IReadOnlyList<GdFolder> input, List<GdFolder> output );
    }

    public class AllFilesToken : FileToken
    {
        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<ScriptableObject> output )
        {
            foreach ( var folder in input )
            {
                output.AddRange( folder.Objects );
            }
        }
    }

    public class AllFilesInDBToken : FileToken
    {
        private readonly GdDb _db;

        public AllFilesInDBToken( GdDb db )
        {
            _db = db;
        }

        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<ScriptableObject> output )
        {
            output.AddRange( _db.AllObjects );
        }
    }

    public class WildcardFilesToken : FileToken
    {
        public readonly StringToken Wildcard;
        private readonly Executor     _executor;

        public WildcardFilesToken( StringToken wildcard, Executor executor )
        {
            Wildcard      = wildcard;
            _executor = executor;
        }

        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<ScriptableObject> output )
        {
            foreach ( var folder in input )
            {
                foreach ( var obj in folder.Objects )
                {
                    if( _executor.MatchString( obj.name, Wildcard ) )
                        output.Add( obj );
                }
            }
        }
    }


    public class AllSubfoldersToken : FolderToken
    {
        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<GdFolder> output )
        {
            foreach ( var folder in input )
            {
                output.AddRange( folder.SubFolders );
            }
        }
    }
    
    public class WildcardSubfoldersToken : FolderToken
    {
        private readonly Executor    _executor;
        private readonly StringToken _wildcard;

        public WildcardSubfoldersToken( StringToken wildcard, Executor executor )
        {
            _wildcard = wildcard;
            _executor = executor;
        }

        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<GdFolder> output )
        {
            foreach ( var folder in input )
            {
                foreach ( var subFolder in folder.SubFolders )
                {
                    if( _executor.MatchString( subFolder.Name, _wildcard ) )
                        output.Add( subFolder );
                }
            }
        }
    }

    public class AllSubfoldersRecursivelyToken : FolderToken
    {
        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<GdFolder> output )
        {
            foreach ( var folder in input )
            {
                foreach ( var subfolder in folder.EnumerateFoldersDFS( true ) )
                {
                    if( !output.Contains( subfolder ) )
                        output.Add( subfolder );
                }
            }
        }
    }
    
    public class AllFoldersInDBToken : FolderToken
    {
        private readonly GdDb _db;

        public AllFoldersInDBToken( GdDb db )
        {
            _db = db;
        }

        public override void ProcessFolder( IReadOnlyList<GdFolder> input, List<GdFolder> output )
        {
            output.AddRange( _db.RootFolder.EnumerateFoldersDFS( true ) );
        }
    }

    public abstract class StringToken
    {
        public StringToken NextToken;

        public abstract Boolean Match( String input, Int32 position );

        public static StringToken Append( params StringToken[] tokens )
        {
            for ( int i = 0; i < tokens.Length - 1; i++ )
            {
                tokens[i].NextToken = tokens[i + 1];
            }

            return tokens[0];
        }
    }

    public class LiteralToken : StringToken
    {
        public readonly String Literal;    //todo Uppercase it and uppercase the input string and compare ordinal

        public LiteralToken( String literal )
        {
            Literal = literal;
        }

        public override Boolean Match( String input, Int32 position )
        {
            if ( NextToken != null )
            {
                if ( position <= input.Length - Literal.Length  )
                {
                    var result = input.IndexOf( Literal, position, Literal.Length, StringComparison.OrdinalIgnoreCase ) == position;
                    if( result)
                        return NextToken.Match( input, position + Literal.Length );
                }
                return false;
            }
            else
            {
                return position == input.Length - Literal.Length && input.EndsWith( Literal, StringComparison.OrdinalIgnoreCase );
            }
        }
    }

    public class AnyTextToken : StringToken
    {
        public override Boolean Match( String input, Int32 position )
        {
            if( input == String.Empty )
                return true;

            if( position >= input.Length )
                return false;

            if( NextToken == null )
                return true;

            for ( int i = position; i < input.Length; i++ )
            {
                 if( NextToken.Match( input, i ) )
                     return true;
            }

            return false;
        }
    }

    public class SomeSymbolToken : StringToken
    {
        public readonly Int32 SymbolsCount;

        public SomeSymbolToken( Int32 symbolsCount = 1 )
        {
            SymbolsCount = symbolsCount;
        }

        public override Boolean Match( String input, Int32 position )
        {
            if ( NextToken != null )
            {
                if ( position <= input.Length - SymbolsCount )
                    return NextToken.Match( input, position + SymbolsCount );
                return false;
            }
            else
            {
                return position == input.Length - SymbolsCount;
            }
        }
    }
}