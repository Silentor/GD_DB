using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor
{
    /// <summary>
    /// Creates gddb DOM from folders and gdo assets structure. Starts from Assets/ folder
    /// </summary>
    public class FoldersParser
    {
        public Folder Root;

        /// <summary>
        /// Parse physical folder structure, root folder goes to <see cref="Root"/>
        /// </summary>
        public void Parse( )
        {
            var folders      = AssetDatabase.FindAssets("t:Folder", new []{"Assets/"});
            var foldersGuids = new List<(Guid, String)>();
            foreach ( var folderId in folders )
            {
                 var guid = Guid.ParseExact( folderId, "N" );
                 var path = AssetDatabase.GUIDToAssetPath( folderId ) + "/";
                 foldersGuids.Add( (guid, path) );
            }

            Root = new Folder() { Name = "Assets/",    FolderGuid = Guid.ParseExact( "A55E7500-F5B6-4EBA-825C-B1BC7331A193", "D" ) } ;

            var gdos = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            foreach ( var guidStr in gdos )
            {
                var path        = AssetDatabase.GUIDToAssetPath(guidStr);

                var splittedPath   = Split( path );
                var folderForAsset = GetFolderForSplittedPath( Root, splittedPath, foldersGuids );
                var guid           = Guid.ParseExact( guidStr, "N" );
                folderForAsset.ObjectIds.Add( guid );
                folderForAsset.Objects.Add( AssetDatabase.LoadAssetAtPath<GDObject>( path ) );
            }

            //Skip unused folders from root
            var checkFolder = Root;
            while( checkFolder.SubFolders.Count == 1 && checkFolder.Objects.Count == 0 )
            {
                checkFolder        = checkFolder.SubFolders[0];
                checkFolder.Parent = null;
                Root               = checkFolder;
            }

            CalculateDepth( Root );
        }

        public void DebugParse( Folder presetHierarchy )
        {
            Root.SubFolders.Add( presetHierarchy );
            presetHierarchy.Parent = Root;

            CalculateDepth( Root );
        }

        public void Print( )
        {
            PrintRecursively( Root, 0 );
        }

        public void CalculateDepth( Folder root )
        {
            foreach ( var folder in root.EnumerateFoldersDFS(  ) )
            {
                var depth       = 0;
                var checkFolder = folder;
                while ( checkFolder.Parent != null )
                {
                    depth++;
                    checkFolder = checkFolder.Parent;
                }
                folder.Depth = depth;
            }
        }

        private void PrintRecursively(Folder folder, int indent )
        {
            var indentStr = new String(' ', indent);
            Debug.Log($"{indentStr}{folder.Name}");
            foreach ( var subFolder in folder.SubFolders )
            {
                PrintRecursively( subFolder, indent + 2 );
            }

            foreach ( var obj in folder.Objects )
            {
                Debug.Log($"  {indentStr}{obj.Name}");
            }
        }

        private Folder GetFolderForSplittedPath( Folder root, IReadOnlyList<String> splittedObjectPath, List<(Guid, String)> folders )
        {
            var existFolder = root;
            foreach ( var pathFolder in splittedObjectPath.Skip( 1 ).SkipLast( 1 ) )    //Skip "Assets" and file name
            {
                var folder = existFolder.SubFolders.Find( f => f.Name == pathFolder );
                if ( folder == null )
                {
                    var path = existFolder.GetHierarchyPath() + pathFolder;
                    var folderGuid = folders.Find( f => f.Item2 == path ).Item1;
                    folder = new Folder { Name = pathFolder, Parent = existFolder, FolderGuid = folderGuid};
                    existFolder.SubFolders.Add( folder );
                }

                existFolder = folder;
            }

            return existFolder;
        }

        //Custom split with preservation of slash at the folders names end
        private IReadOnlyList<String> Split( String assetPath )
        {
            if ( String.IsNullOrEmpty( assetPath ) )
                return Array.Empty<String>();

            var start      = 0;
            var slashIndex = 0;
            var result     = new List<String>();
            while ( start < assetPath.Length )
            {
                slashIndex = assetPath.IndexOf( '/', start );
                if ( slashIndex < 0 )
                {
                    result.Add( assetPath.Substring( start, assetPath.Length - start ) );
                    return result;
                }
                result.Add( assetPath.Substring( start, slashIndex - start + 1 ) );
                start = slashIndex + 1;
            }

            return result;
        }

        
    }
}