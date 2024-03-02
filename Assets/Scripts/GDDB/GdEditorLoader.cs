using System;
using System.Collections.Generic;
using System.IO;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GDDB
{
    public class GdEditorLoader : GdLoader
    {
        public override IReadOnlyList<GDObject> AllObjects => _allObjects;

        private readonly String         _gddbPath;
        private readonly List<GDObject> _allObjects = new List<GDObject>();


        public GdEditorLoader( String name )
        {
 #if !UNITY_EDITOR
                throw new NotSupportedException( "GdEditorLoader can be used only in editor" );            
 #else
                var gdiRootsGuids = AssetDatabase.FindAssets( "t:GDRoot" );
                foreach ( var gdiRootGuid in gdiRootsGuids )
                {
                    var path    = AssetDatabase.GUIDToAssetPath( gdiRootGuid );
                    var gdiRoot = AssetDatabase.LoadAssetAtPath<GDRoot>( path );

                    if( gdiRoot && String.Equals( gdiRoot.Id, name, StringComparison.OrdinalIgnoreCase ) )
                    {
                        _gddbPath = Path.GetDirectoryName( path );
                        Root      = gdiRoot;
                        break;
                    }
                }

                if ( Root == null )
                    throw new ArgumentException( $"GdDB name {name} is incorrect" );

                //Load all gd objects
                var gdObjectGuids = AssetDatabase.FindAssets( "t:GDObject", new[] { _gddbPath });
                foreach ( var gdObjectGuid in gdObjectGuids )
                {
                    var path              = AssetDatabase.GUIDToAssetPath( gdObjectGuid );
                    var gdObjectDirectory = Path.GetDirectoryName( path );
                    if ( gdObjectDirectory.StartsWith( _gddbPath ) )
                    {
                        var gdObject = AssetDatabase.LoadAssetAtPath<GDObject>( path );
                        if( gdObject )
                            _allObjects.Add( gdObject );
                    }
                }

#endif

        } 
    }
   
}
