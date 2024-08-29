using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    /// <summary>
    /// Creates gddb DOM from folders and gdo assets structure
    /// </summary>
    public class FoldersParser
    {
        public readonly Folder Root = new Folder{Name = "Assets", };

        public void Parse( )
        {
            //var folders = AssetDatabase.FindAssets("t:Folder");
            var gdos = AssetDatabase.FindAssets("t:GDObject", new []{"Assets/"});   //Skip packages, consider collect GDObjects from Packages?
            foreach ( var gdoGuid in gdos )
            {
                var path        = AssetDatabase.GUIDToAssetPath(gdoGuid);
                //var gdo = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                //Debug.Log( $"id {gdoGuid}, path {path}, asset {gdo}" );

                var splittedPath = path.Split('/');
                var folderForAsset = GetFolderForSplittedPath( Root, splittedPath );
                folderForAsset.Objects.Add( new GDAsset { AssetGuid = gdoGuid, Asset = AssetDatabase.LoadAssetAtPath<GDObject>( path ) } );
            }
        }

        public void Print( )
        {
            PrintRecursively( Root, 0 );
        }

        private void PrintRecursively(Folder folder, int indent )
        {
            var indentStr = new String(' ', indent);
            Debug.Log($"{indentStr}{folder.Name}/");
            foreach ( var subFolder in folder.SubFolders )
            {
                PrintRecursively( subFolder, indent + 2 );
            }

            foreach ( var obj in folder.Objects )
            {
                Debug.Log($"  {indentStr}{obj.Asset.Name}");
            }
        }

        private Folder GetFolderForSplittedPath( Folder root, String[] splittedPath )
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

        public class Folder
        {
            public String Name;
            public String Path;
            public Folder Parent;

            public List<Folder>  SubFolders = new ();
            public List<GDAsset> Objects    = new();
        }

        public class GDAsset
        {
            public String   AssetGuid;
            public GDObject Asset;
        }
    }
}