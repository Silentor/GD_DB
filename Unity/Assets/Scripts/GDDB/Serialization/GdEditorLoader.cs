using System;
using System.Collections.Generic;
using GDDB.Editor;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// GD DB loader from AssetDatabase in Unity Editor. Used as fast loader for editor play mode without parsing saved DB
    /// </summary>
    public class GdEditorLoader : GdLoader
    {
        /// <summary>
        /// Get all enabled GD Objects
        /// </summary>
        public readonly IReadOnlyList<GdDb.ObjectSearchIndex>   AllObjects;
        public readonly IReadOnlyList<GdFolder> AllFolders;
        public readonly IReadOnlyList<String>   DisabledFolders;
        public readonly String                  RootFolderPath;
        public readonly Int32                   DisabledObjectsCount;

        public GdEditorLoader(  )
        {
 #if !UNITY_EDITOR
                throw new System.NotSupportedException( "GdEditorLoader can be used only in editor" );            
 #else

                var parser  = new GdDbAssetsParser();
                if ( parser.Root != null )
                {
                    _db                  = new GdDb( parser.Root, parser.AllObjects, 0 );
                    AllObjects           = parser.AllObjects;
                    AllFolders           = parser.AllFolders;
                    DisabledFolders      = parser.DisabledFolders;
                    RootFolderPath       = parser.RootFolderPath;
                    DisabledObjectsCount = parser.DisabledObjectsCount;

                    Debug.Log( $"[{nameof(GdEditorLoader)}]-[{nameof(GdEditorLoader)}] loaded GDDB from Editor assets" );
                }
                else
                {
                    Debug.Log( $"[{nameof(GdEditorLoader)}]-[{nameof(GdEditorLoader)}] No GDDB Editor assets found, there is no game design data base in project" );
                }
#endif           

        } 
    }
}
