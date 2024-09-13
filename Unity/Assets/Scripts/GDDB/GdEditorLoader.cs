using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        public GdEditorLoader(  )
        {
 #if !UNITY_EDITOR
                throw new NotSupportedException( "GdEditorLoader can be used only in editor" );            
 #else

                var timer = System.Diagnostics.Stopwatch.StartNew();
                var parser  = new FoldersParser();
                parser.Parse();
                var assetsFolder = parser.Root;

                // if( !gdRoot )
                //     throw new ArgumentException( $"Game design data base name {name} is incorrect" );

                //parser.CalculateDepth( assetsFolder );

                var allObjects = GetAllObjects( assetsFolder );
                _db = new GdDb( assetsFolder, allObjects );

                timer.Stop();

                Debug.Log( $"[GdEditorLoader] GD data base {_db.Name} loaded in {timer.ElapsedMilliseconds} msec" );
#endif           

        } 

        IReadOnlyList<GDObject> GetAllObjects(  Folder root )
        {
            var result = new List<GDObject>();
            foreach ( var folder in root.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdo in folder.Objects )
                {
                    result.Add( gdo );
                }                
            }

            return result;
        }
    }
   
}
