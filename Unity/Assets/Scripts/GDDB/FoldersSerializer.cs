using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDDB
{
    public class FoldersSerializer
    {
        public String Serialize( Folder root, Int32? hash = null )
        {
            var rootJson = SerializeFolder( root, hash );
            return rootJson.ToString( Formatting.Indented );
        }

        public Folder Deserialize( String json )
        {
            var rootJson = JObject.Parse( json );
            return DeserializeFolder( rootJson, null );
        }

        private JObject SerializeFolder( Folder folder, Int32? hash = null )
        {
            var result = new JObject();
            if( hash.HasValue )
                result["hash"] = hash.Value;
            result["name"] = folder.Name;
            result["path"] = folder.Path;
            result["depth"] = folder.Depth;
            result["guid"] = folder.FolderGuid.ToString("D");

            var subFolders = new JArray();
            foreach ( var subFolder in folder.SubFolders )
            {
                subFolders.Add( SerializeFolder( subFolder ) );
            }

            result["subFolders"] = subFolders;

            var objects = new JArray();
            foreach ( var obj in folder.Objects )
            {
                var objJson = new JObject();
                objJson[ "guid" ] = obj.Guid.ToString("D");
                objects.Add( objJson );
            }

            result["objects"] = objects;

            return result;
        }

        private Folder DeserializeFolder( JObject json, Folder? parent )
        {
            var folder = new Folder( json["path"].Value<String>(), json["name"].Value<String>(), Guid.ParseExact( json["guid"].Value<String>(), "D" ) )
            {
                Depth = json["depth"].Value<Int32>(),
                Parent = parent
            };

            var subFolders = json["subFolders"];
            foreach ( var subFolderJson in subFolders )
            {
                var subFolder = DeserializeFolder( ( JObject ) subFolderJson, folder );
                folder.SubFolders.Add( subFolder );
            }

            var objects = json["objects"];
            foreach ( var objJson in objects )
            {
                var objId = Guid.ParseExact( objJson[ "guid" ].Value<String>() , "D" );
                folder.ObjectIds.Add( objId );
            }

            return folder;
        }
    }

    [DebuggerDisplay("Path {Path}, folders {SubFolders.Count}, objects {Objects.Count}")]
    public class Folder
    {
        public readonly String  Name;
        public readonly String  Path;
        public Folder? Parent;
        public Int32   Depth;
        public readonly Guid    FolderGuid;

        public readonly List<Folder>  SubFolders = new ();
        public readonly List<Guid>    ObjectIds    = new();
        public readonly List<GDObject> Objects    = new();

        public Folder( String path, [NotNull] String name, Guid folderGuid )
        {
            if ( String.IsNullOrWhiteSpace( name ) || !IsFolderNameValid( name ) )
                throw new ArgumentException( $"Incorrect folder name '{name}'", nameof(name) );
            if ( String.IsNullOrWhiteSpace( path ) || !IsPathValid( path ) )
                throw new ArgumentException( $"Incorrect path '{path}'", nameof(path) );
            if( folderGuid == Guid.Empty )
                throw new ArgumentException( "Empty guid", nameof(folderGuid) );

            Path = path;
            Name = name;
            FolderGuid = folderGuid;
        }

        public Folder( String name, Guid folderGuid, [NotNull] Folder parent )
        {
            if ( parent == null ) throw new ArgumentNullException( nameof(parent) );
            if ( String.IsNullOrWhiteSpace( name ) || !IsFolderNameValid( name ) )
                throw new ArgumentException( $"Incorrect folder name '{name}'", nameof(name) );
            if( folderGuid == Guid.Empty )
                throw new ArgumentException( "Empty guid", nameof(folderGuid) );

            Path       = String.Concat(parent.Path, "/",  name);
            Name       = name;
            FolderGuid = folderGuid;
            Parent     = parent;

            parent.SubFolders.Add( this );
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
                var folderHash = GetHashString( folder.Path );
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