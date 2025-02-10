using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB.Queries
{
    /// <summary>
    /// Glob-like query parser and executor for GDDB. For query syntax see <see cref="Parser"/>
    /// </summary>
    public class Executor
    {
        public readonly GdDb DB;

        public Executor( GdDb db )
        {
            DB = db;
        }

        public void FindObjects( HierarchyToken query, List<ScriptableObject> resultObjects, List<GdFolder> resultFolders = null )
        {
            if( query == null )
                return;

            var inputFolders = RentFolderList();
            inputFolders.Add( DB.RootFolder );

            var loopDefenerCounter = 0;
            var currentToken = query;
            while ( currentToken != null )
            {
                if ( currentToken is FolderToken folderToken )
                {
                    var outputFolders = RentFolderList();
                    folderToken.ProcessFolder( inputFolders, outputFolders );
                    inputFolders.Clear();
                    inputFolders.AddRange( outputFolders );
                    ReturnFoldersList( outputFolders );
                    currentToken = folderToken.NextToken;
                }
                else if ( currentToken is FileToken fileToken )
                {
                    fileToken.ProcessFolder( inputFolders, resultObjects, resultFolders );
                    ReturnFoldersList( inputFolders );
                    return;
                }

                if( loopDefenerCounter++ > 100 )
                    throw new InvalidOperationException( $"[{nameof(Executor)}]-[{nameof(FindObjects)}] too many loops while processing hierarchy, probably there is a loop in hierarchy tokens" );
            }

            //Should not reach this point for valid query?
            ReturnFoldersList( inputFolders );
        }

        public void FindFolders( HierarchyToken query, List<GdFolder> result )
        {
            if( query == null )
                return;

            var input  = RentFolderList();
            input.Add( DB.RootFolder );

            var loopDefencerCounter = 0;
            var currentToken        = query;
            while ( currentToken != null )
            {
                if ( currentToken is FolderToken folderToken )
                {
                    var outputFolders = RentFolderList();
                    folderToken.ProcessFolder( input, outputFolders );
                    input.Clear();
                    input.AddRange( outputFolders );
                    ReturnFoldersList( outputFolders );
                    currentToken = folderToken.NextToken;
                }

                if( loopDefencerCounter++ > 100 )
                    throw new InvalidOperationException( $"[{nameof(Executor)}]-[{nameof(FindFolders)}] too many loops while processing hierarchy, probably there is a loop in hierarchy tokens" );
            }

            result.AddRange( input );
            ReturnFoldersList( input );
        }


        public Boolean MatchString( String str, StringToken wildcard )
        {
            var position = 0;
            return wildcard.Match( str, position );
        }

        private readonly List<List<GdFolder>>         _folderListsForRent = new (  );
        private readonly List<List<ScriptableObject>> _objectListsForRent = new (  );
        private Int32 _rentedFoldersCount;
        private Int32 _rentedObjectsCount;

        private List<GdFolder> RentFolderList()
        {
            _rentedFoldersCount++;
            if( _folderListsForRent.Count == 0 )
                return new List<GdFolder>(  );
            else
            {
                var result = _folderListsForRent[ ^1 ];
                _folderListsForRent.RemoveAt( _folderListsForRent.Count - 1 );
                return result;
            }
        }

        private void ReturnFoldersList( List<GdFolder> list )
        {
            _rentedFoldersCount--;
            list.Clear();
            _folderListsForRent.Add( list );
        }

        private List<ScriptableObject> RentObjectList()
        {
            _rentedObjectsCount++;
            if( _objectListsForRent.Count == 0 )
                return new List<ScriptableObject>(  );
            else
            {
                var result = _objectListsForRent[ ^1 ];
                _objectListsForRent.RemoveAt( _objectListsForRent.Count - 1 );
                return result;
            }
        }

        private void ReturnObjectList( List<ScriptableObject> list )
        {
            _rentedObjectsCount--;
            list.Clear();
            _objectListsForRent.Add( list );
        }

        public readonly struct ObjectInFolder : IEquatable<ObjectInFolder>
        {
            public readonly GdFolder         Folder;
            public readonly ScriptableObject Object;

            public ObjectInFolder(GdFolder folder, ScriptableObject o )
            {
                Folder      = folder;
                Object = o;
            }

            public void Deconstruct( out GdFolder folder, out ScriptableObject obj )
            {
                folder = Folder;
                obj = Object;
            }

            public bool Equals(ObjectInFolder other)
            {
                return Equals( Folder.FolderGuid, other.Folder.FolderGuid ) && Equals( Object, other.Object );
            }

            public override bool Equals(object obj)
            {
                return obj is ObjectInFolder other && Equals( other );
            }

            public override int GetHashCode( )
            {
                return HashCode.Combine( Folder.FolderGuid, Object );
            }

            public static bool operator ==(ObjectInFolder left, ObjectInFolder right)
            {
                return left.Equals( right );
            }

            public static bool operator !=(ObjectInFolder left, ObjectInFolder right)
            {
                return !left.Equals( right );
            }
        }


    }
}