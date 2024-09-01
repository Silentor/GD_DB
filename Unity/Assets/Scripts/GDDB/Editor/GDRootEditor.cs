using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using File = System.IO.File;
using Random = UnityEngine.Random;

namespace GDDB.Editor
{
    [CustomEditor( typeof(GDRoot), true )]
    public class GDRootEditor : GDObjectEditor
    {
        private GDRoot _target;

        protected override void OnEnable( )
        {
            base.OnEnable();

            _target = (GDRoot)target;
        }

        public override VisualElement CreateInspectorGUI( )
        {
            var result  = base.CreateInspectorGUI();
            var toolbar = new Box()
                          {
                                  style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween }
                          };
            result.Add( toolbar );

            var toJsonBtn = new Button( ( ) => {
                var gddb       = new GdEditorLoader( _target.Id ).GetGameDataBase();
                var assets     = ScriptableObject.CreateInstance<GdAssetReference>();
                var json       = new GDJson().GDToJson( gddb.AllObjects, assets );
                var jsonPath   = Path.Combine( Application.dataPath, $"Resources/{_target.Id}.json" );
                var assetsPath = $"Assets/Resources/{_target.Id}_assets.asset";
                File.WriteAllText( jsonPath, json );
                AssetDatabase.CreateAsset( assets, assetsPath );
                AssetDatabase.Refresh();
                Debug.Log( $"Saved gd DB to json file at {jsonPath}, asset resolver at {assetsPath}" );
            } );
            toJsonBtn.text = "To Json";
            toJsonBtn.style.width = 100;
            toJsonBtn.style.height = 20;
            toolbar.Add( toJsonBtn );

            var toSoBtn = new Button( ( ) => {
                GdScriptablePreprocessBuild.PrepareResourceReference( new GdEditorLoader( _target.Id ).GetGameDataBase() );
            } );
            toSoBtn.text       = "To SO";
            toSoBtn.style.width  = 100;
            toSoBtn.style.height = 20;
            toolbar.Add( toSoBtn );

            var fromJsonBtn = new Button( ( ) => {
                var gdJson = new GdJsonLoader( _target.Id ).GetGameDataBase();
                Debug.Log( $"Loaded gd DB {gdJson.Root.Id}, total objects {gdJson.AllObjects.Count}" );
            } );
            fromJsonBtn.text           = "From Json";
            fromJsonBtn.style.width  = 100;
            fromJsonBtn.style.height = 20;
            toolbar.Add( fromJsonBtn );

            var printHierarchy = new Button( ( ) => 
            {
                var gddb = new GdEditorLoader( _target.Id ).GetGameDataBase();
                gddb.Print();
            } );
            printHierarchy.text         = "Print";
            printHierarchy.style.width  = 100;
            printHierarchy.style.height = 20;
            toolbar.Add( printHierarchy );

            return result;
        }
    }
}