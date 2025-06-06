﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace Gddb
{
    [DebuggerDisplay("Path {GetPath()}, folders {SubFolders.Count}, objects {Objects.Count}")]
    public class GdFolder : IEquatable<GdFolder>
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
        /// <returns>Something like root/folder1/thisfolder</returns>
        public String   GetPath( )
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

        public bool Equals(GdFolder? other)
        {
            if ( other is null ) return false;
            if ( ReferenceEquals( this, other ) ) return true;
            return FolderGuid.Equals( other.FolderGuid );
        }

        public override bool Equals(object? obj)
        {
            if ( obj is null ) return false;
            if ( ReferenceEquals( this, obj ) ) return true;
            if ( obj.GetType() != GetType() ) return false;
            return Equals( (GdFolder) obj );
        }

        public override int GetHashCode( )
        {
            return FolderGuid.GetHashCode();
        }

        public static bool operator ==(GdFolder? left, GdFolder? right)
        {
            return Equals( left, right );
        }

        public static bool operator !=(GdFolder? left, GdFolder? right)
        {
            return !Equals( left, right );
        }
    }
}