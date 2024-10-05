using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace GDDB
{
    [DebuggerDisplay("Path {GetPath()}, folders {SubFolders.Count}, objects {Objects.Count}")]
    public class Folder
    {
        public readonly String  Name;
        //public          String  Path { get; internal set; }
        public          Folder? Parent;
        public          Int32   Depth;
        public readonly Guid    FolderGuid;

        

        public readonly List<Folder>   SubFolders = new ();
        public readonly List<Guid>     ObjectIds  = new();
        public readonly List<GDObject> Objects    = new();

        public Folder( [NotNull] String name, Guid folderGuid )
        {
            if ( String.IsNullOrWhiteSpace( name ) || !IsFolderNameValid( name ) )
                throw new ArgumentException( $"Incorrect folder name '{name}'", nameof(name) );
            if( folderGuid == Guid.Empty )
                throw new ArgumentException( "Empty guid", nameof(folderGuid) );

            Name       = name;
            FolderGuid = folderGuid;
        }

        public Folder( String name, Guid folderGuid, [NotNull] Folder parent )
        {
            if ( parent == null ) throw new ArgumentNullException( nameof(parent) );
            if ( String.IsNullOrWhiteSpace( name ) || !IsFolderNameValid( name ) )
                throw new ArgumentException( $"Incorrect folder name '{name}'", nameof(name) );
            if( folderGuid == Guid.Empty )
                throw new ArgumentException( "Empty guid", nameof(folderGuid) );

            Name       = name;
            FolderGuid = folderGuid;
            Parent     = parent;
            Depth      = parent.Depth + 1;

            parent.SubFolders.Add( this );
        }

        public String GetPath( )
        {
            //Some fast passes
            if( Parent == null )
                return Name;
            else if( Depth == 1)
                return String.Concat( Parent.Name, "/", Name );
            else if ( Depth == 2 )
                return String.Concat( Parent.Parent.Name, "/", Parent.Name, "/", Name );

            else return String.Concat( Parent.GetPath(),         "/", Name );
        }

        public IEnumerable<Folder> EnumerateFoldersDFS( Boolean includeSelf = true )
        {
            if( includeSelf )
                yield return this;
            foreach ( var subFolder in SubFolders )
            {
                foreach ( var subSubFolder in subFolder.EnumerateFoldersDFS(  ) )
                {
                    yield return subSubFolder;
                }
            }
        }

        public Folder GetRootFolder( )
        {
            if( Parent == null )
                return this;

            return Parent.GetRootFolder();
        }

        /// <summary>
        /// Get hash of folders tree structure
        /// </summary>
        /// <returns></returns>
        public Int32 GetFoldersStructureHash( )
        {
            var      result   = 0;
            foreach ( var folder in EnumerateFoldersDFS(  ) )
            {
                var folderHash = unchecked( folder.FolderGuid.GetHashCode() + GetHashString( folder.GetPath() ) );
                unchecked
                {
                    result = result * 31 + folderHash;
                }
            }

            return result;
        }

        private static Boolean IsFolderNameValid( String folderName )
        {
            if ( String.IsNullOrWhiteSpace( folderName ) )
                return false;
            foreach ( var invalidChar in InvalidFolderNameChars )
            {
                if( folderName.Contains( invalidChar ) )
                    return false;
            }

            return true;
        }

        private static Boolean IsPathValid( String path )
        {
            if ( String.IsNullOrWhiteSpace( path ) )
                return false;
            foreach ( var invalidChar in InvalidPathChars )
            {
                if( path.Contains( invalidChar ) )
                    return false;
            }

            return true;
        }

        private Int32 GetHashString( string text )
        {
            if ( String.IsNullOrEmpty( text ) )
                return 0;

            unchecked
            {
                var hash = 23;
                foreach (var c in text)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        private static readonly Char[] InvalidFolderNameChars = System.IO.Path.GetInvalidPathChars()
                                                                      .Concat( new []{'/', '\\', '*', ':'} )     //Unity limitation
                                                                      .ToArray();
        private static readonly Char[] InvalidPathChars = System.IO.Path.GetInvalidPathChars()
                                                                .Concat( new []{     '\\', '*', ':'} )          //Unity limitation
                                                                .ToArray();
    }
}