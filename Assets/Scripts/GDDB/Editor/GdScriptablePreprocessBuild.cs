using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GDDB.Editor
{
    public class GdScriptablePreprocessBuild : IPreprocessBuildWithReport
    {
        public Int32 callbackOrder { get; } = 0;

        public void  OnPreprocessBuild( BuildReport report )
        {
            //Prepare Resource folder references to GD DB
            var gdiRootsGuids = AssetDatabase.FindAssets( "t:GDRoot" );
            foreach ( var gdiRootGuid in gdiRootsGuids )
            {
                var path    = AssetDatabase.GUIDToAssetPath( gdiRootGuid );
                var gdiRoot = AssetDatabase.LoadAssetAtPath<GDRoot>( path );

                if( gdiRoot )
                {
                    var gddb = new GdEditorLoader( gdiRoot.Id ).GetGameDataBase();
                    PrepareResourceReference( gddb );

                    // if( AssetDatabase.CopyAsset( path, $"Assets/Resources/{gdiRoot.Id}.asset" ) )
                    // {
                    //     var gdrootCopy = AssetDatabase.LoadAssetAtPath<GDRoot>( $"Assets/Resources/{gdiRoot.Id}.asset" );
                    //     EditorUtility.SetDirty( gdrootCopy );
                    //     AssetDatabase.SaveAssetIfDirty( gdrootCopy );
                    // }
                    // else
                    // {
                    //     throw new  BuildFailedException($"[GdScriptablePreprocessBuild] Failed to copy {path} to Resources folder.");
                    // }
                }
            }
        }

        public static void PrepareResourceReference( GdDb gddb )
        {
            var gdReference = ScriptableObject.CreateInstance<GdScriptableReference>();
            gdReference.Root    = gddb.Root;
            gdReference.Content = gddb.AllObjects.ToList();
            AssetDatabase.CreateAsset( gdReference, $"Assets/Resources/{gddb.Root.Id}.asset");
        }
    }
}
