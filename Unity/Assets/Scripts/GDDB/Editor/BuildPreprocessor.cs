using System;
using System.IO;
using System.Linq;
using GDDB.Serialization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GDDB.Editor
{
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public Int32 callbackOrder { get; } = 0;

        public static event Action BuildPreprocessing;

        public void  OnPreprocessBuild( BuildReport report )
        {
            BuildPreprocessing?.Invoke();

            //Prepare Resource folder references to GD DB
            var gdiRootsGuids = AssetDatabase.FindAssets( "t:GDRoot" );
            foreach ( var gdiRootGuid in gdiRootsGuids )
            {
                var path    = AssetDatabase.GUIDToAssetPath( gdiRootGuid );
                var gdiRoot = AssetDatabase.LoadAssetAtPath<GDRoot>( path );

                if( gdiRoot )
                {
                    var gddb = new GdEditorLoader(  ).GetGameDataBase();
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
            var serializer = new DBAssetSerializer();
            var rootFolder = gddb.RootFolder;
            var asset      = serializer.Serialize( rootFolder );
            var path       = $"Assets/Resources/{gddb.Name}.folders.asset";
            AssetDatabase.CreateAsset( asset, path);

            Debug.Log( $"[{nameof(BuildPreprocessor)}]-[{nameof(PrepareResourceReference)}] Saved asset GDDB to {path}" );
        }
    }
}
