using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace GDDB.Editor
{
    [CustomEditor( typeof(GDObject), true )]
    public class GDObjectEditor : UnityEditor.Editor
    {
        private GDObject _target;
        private Int32    _lastSelectedComponentIndex = -1;
        private Material _componentDelimiterMat;

        protected virtual void OnEnable( )
        {
            _target = (GDObject)target;
            var shader = Shader.Find("Hidden/Internal-Colored");
            _componentDelimiterMat    = new Material(shader);
        }

        protected virtual void OnDisable( )
        {
            DestroyImmediate( _componentDelimiterMat );
        }

        public override void OnInspectorGUI( )
        {
            //base.OnInspectorGUI();

            var so        = serializedObject;
            so.UpdateIfRequiredOrScript();

            var scriptProp = so.FindProperty( "m_Script" );
            var guidProp = so.FindProperty( "SerGuid" );
            var compsProp = so.FindProperty( "Components" );

            //Draw GDObject properties
            using (new EditorGUI.DisabledScope( true )  )
                EditorGUILayout.PropertyField(scriptProp, true);

            // var bytes  = _target.Guid.ToByteArray();
            // var hexStr = String.Empty;
            // foreach ( var b in bytes )
            // {
            //     hexStr += b.ToString( "x2" );
            // }
            //EditorGUILayout.LabelField( "Guid", $"{_target.Guid.ToString()} {hexStr}" );
            EditorGUILayout.LabelField( "Guid", $"{_target.Guid.ToString()}" );

            //Draw properties of GDObject descendants

            var gdObjectProp = so.GetIterator();
            for (var enterChildren = true; gdObjectProp.NextVisible(enterChildren); enterChildren = false)
            {
                //Hide completely, custom draw
                if ( SerializedProperty.EqualContents( gdObjectProp, compsProp) || SerializedProperty.EqualContents( gdObjectProp, scriptProp ) )
                    continue;
            
                EditorGUILayout.PropertyField(gdObjectProp, true);
            }

            if ( GUILayout.Button( "Dirty and save", GUILayout.Width( 100 ) ) )
            {
                EditorUtility.SetDirty( _target );
                AssetDatabase.SaveAssetIfDirty( _target );
                return;
            }

            DrawDelimiter( 10, 5 );

            //Draw GDComponents list
            for ( int i = 0; i < compsProp.arraySize; i++ )
            {
                var foldoutStatus = DrawComponentHeader( compsProp, i );

                if ( foldoutStatus )
                {
                    var compProp    = compsProp.GetArrayElementAtIndex( i );
                    var compEndProp = compProp.GetEndProperty();
                    if ( compProp.NextVisible( true ) && !SerializedProperty.EqualContents( compProp, compEndProp ) )
                    {
                        do
                        {
                            EditorGUILayout.PropertyField( compProp );
                        }   
                        while ( compProp.NextVisible( false ) && !SerializedProperty.EqualContents( compProp, compEndProp ) );
                    }

                    DrawDelimiter( 10, 5 );
                }
            }

            so.ApplyModifiedProperties();

            DrawComponentAddBtn();
        }

        private Boolean DrawComponentHeader( SerializedProperty components, Int32 componentIndex )
        {
            var compProp = components.GetArrayElementAtIndex( componentIndex );

            GUILayout.BeginHorizontal(  );

            var foldStatus = true;

            if ( compProp.managedReferenceValue != null )
            {
                var compType = compProp.managedReferenceValue.GetType();

                foldStatus = IsComponentFoldout( compProp.managedReferenceValue.GetType() );
                EditorGUI.BeginChangeCheck();
                foldStatus = EditorGUILayout.Foldout( foldStatus, /*GUIContent.none*/compType.Name, true , Styles.Foldout );
                if( EditorGUI.EndChangeCheck() )
                    SetComponentFoldout( compProp.managedReferenceValue.GetType(), foldStatus );
                
                //var componentTypeName = compProp.managedReferenceFullTypename;
                //componentTypeName = !String.IsNullOrEmpty(componentTypeName) ? componentTypeName.Split('.').Last() : "Null";
                //GUILayout.Label( componentTypeName, Styles.BoldLabel );
                GUILayout.FlexibleSpace();
                GUILayout.Label( $"({compType.FullName}, {compType.Assembly.GetName().Name})", Styles.ItalicLabel );
                if( GUILayout.Button( "X" ) )
                {
                    RemoveComponent( componentIndex );
                }
            }
            else
            {
                if( String.IsNullOrEmpty( compProp.managedReferenceFullTypename) )
                    EditorGUILayout.HelpBox( "Component type cannot be found", MessageType.Error, wide: true);
                else
                    EditorGUILayout.HelpBox( "Component is null", MessageType.Error, wide: true);

                GUILayout.FlexibleSpace();
                if( GUILayout.Button( "X" ) )
                {
                    RemoveComponent( componentIndex );
                }
            }
            GUILayout.EndHorizontal();

            return foldStatus;
        }

        private void DrawComponentAddBtn( )
        {
            GUILayout.BeginHorizontal(  );

            GUILayout.Label( "Add component", EditorStyles.boldLabel );

            var allComponentTypes   = TypeCache.GetTypesDerivedFrom( typeof(GDComponent) );
            var allProperComponents = allComponentTypes.Where( t => !t.IsAbstract ).OrderBy( t => t.Name ).ToArray();
            var allProperNames = allProperComponents.Select( t => t.Name ).ToArray();
            _lastSelectedComponentIndex = EditorGUILayout.Popup( _lastSelectedComponentIndex, allProperNames );

            using (new EditorGUI.DisabledScope( _lastSelectedComponentIndex < 0 ))
            {
                if ( GUILayout.Button( "Add", GUILayout.Width( 100 ) ) )
                {
                    if( _lastSelectedComponentIndex >= 0 )
                    {
                        var typeToAdd = allProperComponents[ _lastSelectedComponentIndex ];
                        AddComponent( typeToAdd );
                    }
                }
            }

            GUILayout.EndHorizontal();
        }         

        private void DrawDelimiter( Single spaceBefore, Single spaceAfter )
        {
            Rect rect = GUILayoutUtility.GetRect(10, 1000, spaceBefore + spaceAfter, spaceBefore + spaceAfter);
            if ( Event.current.type == EventType.Repaint )
            {
                GUI.BeginClip( rect );
                GL.PushMatrix();
                GL.Clear( true, false, Color.black );

                _componentDelimiterMat.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Color(GUI.skin.settings.cursorColor);
                GL.Vertex3(0,          spaceBefore, 0);
                GL.Vertex3(rect.width, spaceBefore, 0);
                GL.End();

                GL.PopMatrix();
                GUI.EndClip();
            }
        }

        private void AddComponent( Type componentType )
        {
            var newComponent = Activator.CreateInstance( componentType );
            _target.Components.Add( (GDComponent)newComponent );
            EditorUtility.SetDirty( _target );
        }

        private void RemoveComponent( Int32 componentIndex )
        {
            _target.Components.RemoveAt( componentIndex );
            EditorUtility.SetDirty( _target );
        }

        private Boolean IsComponentFoldout( Type componentType )
        {
            return  EditorPrefs.GetBool( componentType.Name, true );
        }

        private void SetComponentFoldout( Type componentType, Boolean state )
        {
            EditorPrefs.SetBool( componentType.Name, state );
        }


        private static class Styles
        {
            public static readonly GUIStyle ItalicLabel        = new ( EditorStyles.label ) { fontStyle = FontStyle.Italic };
            public static readonly GUIStyle BoldLabel          = EditorStyles.boldLabel;
            public static readonly GUIStyle ComponentDelimiter = new ( GUI.skin.box ) { padding = new RectOffset()/*normal = new GUIStyleState(){background = Texture2D.blackTexture} */};
            public static readonly GUIStyle Foldout = new ( EditorStyles.foldout ) { fontStyle = FontStyle.Bold };
        }
    }
}