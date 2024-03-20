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

        public override void OnInspectorGUI( )
        {
            base.OnInspectorGUI();

            GUILayout.BeginHorizontal(  );
            if ( GUILayout.Button( "ToJson", GUILayout.Width( 100 ) ) )
            {
                var gddb = new GdEditorLoader( _target.Id );
                var json = new GDJson().GDToJson( gddb.AllObjects );
                var path = Path.Combine( Application.dataPath, $"Resources/{_target.Id}.json" );
                File.WriteAllText( path, json );
                AssetDatabase.Refresh();
                Debug.Log( $"Saved gd DB to json file at {path}" );
            }

            if ( GUILayout.Button( "ToSO", GUILayout.Width( 100 ) ) )
            {
                GdScriptablePreprocessBuild.PrepareResourceReference( new GdEditorLoader( _target.Id ) );
            }

            GUILayout.FlexibleSpace();

            if ( GUILayout.Button( "Test from Json", GUILayout.Width( 100 ) ) )
            {
                var gdJson = new GdJsonLoader( _target.Id );
                Debug.Log( $"Loaded gd DB {gdJson.Root.Id}, total objects {gdJson.AllObjects.Count}" );
            }
            GUILayout.EndHorizontal();
        }
    }
}