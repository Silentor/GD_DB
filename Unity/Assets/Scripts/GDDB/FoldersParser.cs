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
        public readonly Folder Root = new Folder{Name = "Assets/", };

        /// <summary>
        /// Parse real folder structure, root folder goes to <see cref="Root"/>
        /// </summary>
        public void Parse( )
        {
            //var folders = AssetDatabase.FindAssets("t:Folder");
            var gdos = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            foreach ( var guidStr in gdos )
            {
                var path        = AssetDatabase.GUIDToAssetPath(guidStr);
                //var gdo = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                //Debug.Log( $"id {gdoGuid}, path {path}, asset {gdo}" );

                var splittedPath   = Split( path );
                var folderForAsset = GetFolderForSplittedPath( Root, splittedPath );
                var guid           = Guid.ParseExact( guidStr, "N" );
                folderForAsset.Objects.Add( new GDAsset { AssetGuid = guid, Asset = AssetDatabase.LoadAssetAtPath<GDObject>( path ) } );
            }

            //Skip unused folders?

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
                Debug.Log($"  {indentStr}{obj.Asset.Name}");
            }
        }

        private Folder GetFolderForSplittedPath( Folder root, IReadOnlyList<String> splittedPath )
        {
            var existFolder = root;
            foreach ( var pathFolder in splittedPath.Skip( 1 ) )    //Skip "Assets"
            {
                if ( pathFolder == splittedPath.Last() )
                    break;

                var folder = existFolder.SubFolders.Find( f => f.Name == pathFolder );
                if ( folder == null )
                {
                    folder = new Folder { Name = pathFolder, Parent = existFolder };
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