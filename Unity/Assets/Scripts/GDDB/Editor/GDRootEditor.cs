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

            var toJsonBtn = new Button( ( ) => 
            {
                var freshDB = new GdEditorLoader( _target.Id ).GetGameDataBase();

                //Save hierarchy to json
                var serializer = new FoldersSerializer();
                var json       = serializer.Serialize( freshDB.RootFolder );
                var structureJsonPath   = Path.Combine( Application.dataPath, $"Resources/{_target.Id}.structure.json" );
                File.WriteAllText( structureJsonPath, json );

                //Save gd objects to another json
                var gdSerializer = new GDJson();
                var referencedAssets       = ScriptableObject.CreateInstance<GdAssetReference>();
                json = gdSerializer.GDToJson( freshDB.AllObjects, referencedAssets );
                var objectsJsonPath = Path.Combine( Application.dataPath, $"Resources/{_target.Id}.objects.json" );
                File.WriteAllText( objectsJsonPath, json );

                //Save referenced assets to scriptable object list
                var assetsPath = $"Assets/Resources/{_target.Id}.assets.asset";
                AssetDatabase.CreateAsset( referencedAssets, assetsPath );

                AssetDatabase.Refresh();

                Debug.Log( $"Saved gddb structure to {structureJsonPath}, gddb objects to {objectsJsonPath}, asset resolver to {assetsPath}" );
            } );
            toJsonBtn.text = "DB to Json";
            toJsonBtn.style.width = 100;
            toJsonBtn.style.height = 20;
            toolbar.Add( toJsonBtn );

            var toSoBtn = new Button( ( ) => {
                GdScriptablePreprocessBuild.PrepareResourceReference( new GdEditorLoader( _target.Id ).GetGameDataBase() );
            } );
            toSoBtn.text       = "DB to SO";
            toSoBtn.style.width  = 100;
            toSoBtn.style.height = 20;
            toolbar.Add( toSoBtn );

            var fromJsonBtn = new Button( ( ) => {
                var gdJson = new GdJsonLoader( _target.Id ).GetGameDataBase();
                Debug.Log( $"Loaded gd DB {gdJson.Root.Id}, total objects {gdJson.AllObjects.Count}" );
                gdJson.Print();
            } );
            fromJsonBtn.text           = "DB from Json";
            fromJsonBtn.style.width  = 100;
            fromJsonBtn.style.height = 20;
            toolbar.Add( fromJsonBtn );

            var printHierarchy = new Button( ( ) => 
            {
                var gddb = new GdEditorLoader( _target.Id ).GetGameDataBase();
                gddb.Print();
            } );
            printHierarchy.text         = "Print editor DB";
            printHierarchy.style.width  = 100;
            printHierarchy.style.height = 20;
            toolbar.Add( printHierarchy );

            return result;
        }
    }
}