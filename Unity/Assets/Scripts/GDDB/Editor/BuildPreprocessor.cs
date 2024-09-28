using System;
using System.IO;
using System.Linq;
using GDDB.GDDB;
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
            var gdReference = ScriptableObject.CreateInstance<GdScriptableReference>();
            //gdReference.Root    = gddb.Root;
            gdReference.Content = gddb.AllObjects.Select( gdo => new GDObjectAndGuid(){Object = gdo, Guid = new SerializableGuid(){Guid = gdo.Guid}} ).ToArray();
            AssetDatabase.CreateAsset( gdReference, $"Assets/Resources/{gddb.Name}.objects.asset");

            var serializer = new FoldersSerializer();
            var json       = serializer.Serialize( gddb.RootFolder );
            var jsonPath   = Path.Combine( Application.dataPath, $"Resources/{gddb.Name}.structure.json" );
            File.WriteAllText( jsonPath, json );
            AssetDatabase.Refresh();
        }
    }
}
