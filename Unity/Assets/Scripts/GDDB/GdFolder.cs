#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace GDDB
{
    [DebuggerDisplay("Path {GetPath()}, folders {SubFolders.Count}, objects {Objects.Count}")]
    public class GdFolder
    {
        public readonly String  Name;
        //public          String  Path { get; internal set; }
        public          GdFolder? Parent;
        public          Int32   Depth;
        public readonly Guid    FolderGuid;

        public readonly List<GdFolder>          SubFolders = new ();
        public readonly List<ScriptableObject>  Objects    = new();

        public GdFolder( [NotNull] String name, Guid folderGuid )
        {
            if ( String.IsNullOrWhiteSpace( name ) || !IsFolderNameValid( name ) )
                throw new ArgumentException( $"Incorrect folder name '{name}'", nameof(name) );
            if ( folderGuid == Guid.Empty )
                throw new ArgumentException(  $"Empty guid for folder {name}", nameof(folderGuid) );

            Name       = name;
            FolderGuid = folderGuid;
        }

        public GdFolder( String name, Guid folderGuid, [NotNull] GdFolder parent )
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

        /// <summary>
        /// Get path relative to DB root folder
        /// </summary>
        /// <returns></returns>
        public String GetPath( )
        {
            //Some fast passes
            if( Parent == null )
                return Name;
            else if( Depth == 1)
                return String.Concat( Parent.Name, "/", Name );
            else if ( Depth == 2 )
                return String.Concat( Parent.Parent!.Name, "/", Parent.Name, "/", Name );

            else return String.Concat( Parent.GetPath(),  "/", Name );
        }

        public IEnumerable<GdFolder> EnumeratePath( )
        {
            var current = this;
            while ( current != null )
            {
                yield return current;
                current = current.Parent;
            }
        }

        public IEnumerable<GdFolder> EnumerateFoldersDFS( Boolean includeSelf = true )
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

        public GdFolder GetRootFolder( )
        {
            if( Parent == null )
                return this;

            return Parent.GetRootFolder();
        }

        /// <summary>
        /// Get hash of folders tree structure (ignore objects)
        /// </summary>
        /// <returns></returns>
        public UInt64 GetFoldersChecksum( )
        {
            var      result   = 0ul;
            foreach ( var folder in EnumerateFoldersDFS(  ) )
            {
                unchecked
                {
                    var folderHash = (UInt64)folder.FolderGuid.GetHashCode() + GetStringChecksum( folder.GetPath() )/* + (UInt64)Objects.Count*/;
                    result += folderHash;
                }
            }

            return result;
        }

        public String ToHierarchyString( )
        {
            var result = new StringBuilder();
            ToHierarchyString( 0, result );
            return result.ToString();
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

        private UInt64 GetStringChecksum( string text )
        {
            if ( String.IsNullOrEmpty( text ) )
                return 0;

            unchecked
            {
                var hash = 0ul;
                foreach (var c in text)
                {
                    hash = hash * 3 + c;
                }
                return hash;
            }
        }

        private void ToHierarchyString( Int32 indent, StringBuilder result )
        {
            result.Append( ' ', indent );
            result.Append( Name );
            result.AppendLine( "/" );

            var childIndent = indent + 2;
            foreach ( var subFolder in SubFolders )
            {
                subFolder.ToHierarchyString( childIndent, result );
            }

            foreach ( var obj in Objects )
            {
                result.Append( ' ', childIndent );
                result.AppendLine( obj.name );
            }
        }

        private static readonly Char[] InvalidFolderNameChars = System.IO.Path.GetInvalidPathChars()
                                                                      .Concat( new []{'/', '\\', '*', ':'} )     //Unity limitation
                                                                      .ToArray();
        private static readonly Char[] InvalidPathChars = System.IO.Path.GetInvalidPathChars()
                                                                .Concat( new []{     '\\', '*', ':'} )          //Unity limitation
                                                                .ToArray();

        public class GuidComparer : IEqualityComparer<GdFolder>
        {
            public static readonly GuidComparer Instance = new GuidComparer();

            public Boolean Equals( GdFolder x, GdFolder y )
            {
                return x.FolderGuid == y.FolderGuid;
            }

            public Int32 GetHashCode( GdFolder obj )
            {
                return obj.FolderGuid.GetHashCode();
            }
        }
    }
}