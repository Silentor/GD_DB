using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB.Queries
{
    public class Executor
    {
        public readonly GdDb DB;

        public Executor( GdDb db )
        {
            DB = db;
        }

        public IReadOnlyList<ScriptableObject> FindObjects( HierarchyToken token )
        {
            var input = new List<GdFolder>(  ){DB.RootFolder};
            var outputFolders = new List<GdFolder>(  );

            var loopDefencerCounter = 0;
            var currentToken = token;
            while ( currentToken != null )
            {
                if ( currentToken is FolderToken folderToken )
                {
                    outputFolders.Clear();
                    folderToken.ProcessFolder( input, outputFolders );
                    input.Clear();
                    input.AddRange( outputFolders );
                    currentToken = folderToken.NextToken;
                }
                else if ( currentToken is FileToken fileToken )
                {
                    var outputObjects = new List<ScriptableObject>(  );
                    fileToken.ProcessFolder( input, outputObjects );
                    return outputObjects;
                }

                if( loopDefencerCounter++ > 100 )
                    throw new InvalidOperationException( $"[{nameof(Executor)}]-[{nameof(FindObjects)}] too many loops while processing hierarchy, probably there is a loop in hierarchy tokens" );
            }

            return Array.Empty<ScriptableObject>();
        }

        public IReadOnlyList<GdFolder> FindFolders( HierarchyToken token )
        {
            var input         = new List<GdFolder>(  ){DB.RootFolder};
            var outputFolders = new List<GdFolder>(  );

            var loopDefencerCounter = 0;
            var currentToken        = token;
            while ( currentToken != null )
            {
                if ( currentToken is FolderToken folderToken )
                {
                    outputFolders.Clear();
                    folderToken.ProcessFolder( input, outputFolders );
                    input.Clear();
                    input.AddRange( outputFolders );
                    currentToken = folderToken.NextToken;
                }

                if( loopDefencerCounter++ > 100 )
                    throw new InvalidOperationException( $"[{nameof(Executor)}]-[{nameof(FindFolders)}] too many loops while processing hierarchy, probably there is a loop in hierarchy tokens" );

            }

            return outputFolders;
        }


        public Boolean MatchString( String str, StringToken wildcard )
        {
            var position = 0;
            return wildcard.Match( str, position );
        }

    }
}