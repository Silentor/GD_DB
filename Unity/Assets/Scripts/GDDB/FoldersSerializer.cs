﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDDB
{
    public class FoldersSerializer
    {
        public String Serialize( Folder root )
        {
            var rootJson = SerializeFolder( root );
            return rootJson.ToString( Formatting.Indented );
        }

        public Folder Deserialize( String json )
        {
            var rootJson = JObject.Parse( json );
            return DeserializeFolder( rootJson, null );
        }

        private JObject SerializeFolder( Folder folder )
        {
            var result = new JObject();
            result["name"] = folder.Name;
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
            var folder = new Folder
            {
                Name = json["name"].Value<String>(),
                Depth = json["depth"].Value<Int32>(),
                FolderGuid = Guid.ParseExact( json["guid"].Value<String>(), "D"),
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

        public IEnumerable<Folder> Flatten( Folder root )
        {
            return root.EnumerateFoldersDFS(  );
        }
    }

    [DebuggerDisplay("{Asset.Name}")]
    public class GDAsset
    {
        public Guid   AssetGuid;
        //public String   AssetName;
        public GDObject Asset;

        // public String GetName( )
        // {
        //     return Asset ? Asset.Name : AssetGuid;
        // }

        // public GDObject GetObject( )
        // {
        //     if ( !Asset )
        //     {
        //         var path = AssetDatabase.GUIDToAssetPath( AssetGuid );
        //         Asset = AssetDatabase.LoadAssetAtPath<GDObject>( path );
        //     }
        //
        //     return Asset;
        // }
    }

    [DebuggerDisplay("Name {GetHierarchyPath()}, folders {SubFolders.Count}, objects {Objects.Count}")]
    public class Folder
    {
        public String  Name;
        public Folder? Parent;
        public Int32   Depth;
        public Guid    FolderGuid;

        public String PartName => Name.Substring(0, Name.Length - 1);

        public readonly List<Folder>  SubFolders = new ();
        public readonly List<Guid>    ObjectIds    = new();
        public readonly List<GDObject> Objects    = new();
            
        public String GetHierarchyPath( )
        {
            var path   = Name;
            var parent = Parent;
            while ( parent != null )
            {
                path   = parent.Name + path;
                parent = parent.Parent;
            }

            return path;
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
    }
}