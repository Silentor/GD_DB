using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using GDDB.Editor;
#endif

namespace GDDB
{
    /// <summary>
    /// GD DB loader from AssetDatabase in Unity Editor
    /// </summary>
    public class GdEditorLoader : GdLoader
    {
        private readonly String         _gddbPath;

        public GdEditorLoader( String name )
        {
 #if !UNITY_EDITOR
                throw new NotSupportedException( "GdEditorLoader can be used only in editor" );            
 #else

                var parser  = new FoldersParser();
                parser.Parse();
                var allGDOfolders = parser.Root;

                Folder gddbFolder = null;
                GDRoot gdRoot     = null;
                foreach ( var folder in allGDOfolders.EnumerateFoldersDFS(  ) )
                {
                    foreach ( var gdAsset in folder.Objects )
                    {
                        var asset = gdAsset.Asset;
                        if( asset is GDRoot gdR && String.Equals( gdR.Id, name, StringComparison.OrdinalIgnoreCase ) )
                        {
                            gddbFolder        = folder;
                            gddbFolder.Parent = null;
                            gdRoot            = gdR;
                            break;
                        }
                    }
                }

                if( !gdRoot )
                    throw new ArgumentException( $"Game design data base name {name} is incorrect" );

                parser.CalculateDepth( gddbFolder );

                var allObjects = GetAllObjects( gddbFolder );
                _db = new GdDb( gddbFolder, allObjects );
#endif           

        } 

        IReadOnlyList<GDObject> GetAllObjects(  Folder root )
        {
            var result = new List<GDObject>();
            foreach ( var folder in root.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdo in folder.Objects )
                {
                    result.Add( gdo.Asset );
                }                
            }

            return result;
        }
    }
   
}
