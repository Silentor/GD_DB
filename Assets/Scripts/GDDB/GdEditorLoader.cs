using System;
using System.Collections.Generic;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
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
                GDRoot root = null;

                var gdiRootsGuids = AssetDatabase.FindAssets( "t:GDRoot" );
                foreach ( var gdiRootGuid in gdiRootsGuids )
                {
                    var path    = AssetDatabase.GUIDToAssetPath( gdiRootGuid );
                    var gdiRoot = AssetDatabase.LoadAssetAtPath<GDRoot>( path );

                    if( gdiRoot && String.Equals( gdiRoot.Id, name, StringComparison.OrdinalIgnoreCase ) )
                    {
                        _gddbPath = Path.GetDirectoryName( path );
                        root      = gdiRoot;
                        break;
                    }
                }

                if ( !root )
                    throw new ArgumentException( $"GdDB name {name} is incorrect" );

                //Load all internal gd objects
                var allObjects = new List<GDObject>();

                var gdObjectGuids = AssetDatabase.FindAssets( "t:GDObject", new[] { _gddbPath });
                foreach ( var gdObjectGuid in gdObjectGuids )
                {
                    var path              = AssetDatabase.GUIDToAssetPath( gdObjectGuid );
                    var gdObjectDirectory = Path.GetDirectoryName( path );
                    if ( gdObjectDirectory.StartsWith( _gddbPath ) )
                    {
                        var gdObject = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                        if( gdObject )
                            allObjects.Add( gdObject );
                    }
                }

                _db = new GdDb( allObjects );
#endif           

        } 
    }
   
}
