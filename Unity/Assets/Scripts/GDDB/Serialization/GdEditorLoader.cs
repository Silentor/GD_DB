using System;
using System.Collections.Generic;
using GDDB.Editor;
using UnityEngine;

namespace GDDB.Serialization
{
    /// <summary>
    /// GD DB loader from AssetDatabase in Unity Editor. Used as fast loader for editor play mode without parsing saved DB. Logic sits in FolderParser class
    /// </summary>
    public class GdEditorLoader : GdLoader
    {
        /// <summary>
        /// Get all enabled GD Objects
        /// </summary>
        public readonly IReadOnlyList<ScriptableObject> AllObjects;
        public readonly IReadOnlyList<GdFolder>         AllFolders;

        public GdEditorLoader(  )
        {
 #if !UNITY_EDITOR
                throw new System.NotSupportedException( "GdEditorLoader can be used only in editor" );            
 #else

                var parser  = new GdDbAssetsParser();
                if ( parser.Parse() )
                {
                    // if( !gdRoot )
                    //     throw new ArgumentException( $"Game design data base name {name} is incorrect" );

                    _db        = new GdDb( parser.Root, parser.AllObjects, 0 );
                    AllObjects = parser.AllObjects;
                    AllFolders = parser.AllFolders;

                    Debug.Log( $"[{nameof(GdEditorLoader)}]-[{nameof(GdEditorLoader)}] loaded GDDB from assets" );
                }
                else
                {
                    _db        = new GdDb( parser.Root, parser.AllObjects, 0 );
                    AllObjects = parser.AllObjects;
                    AllFolders = parser.AllFolders;
                    Debug.Log( $"[{nameof(GdEditorLoader)}]-[{nameof(GdEditorLoader)}] No GDDB assets found" );
                }
#endif           

        } 
    }
}
