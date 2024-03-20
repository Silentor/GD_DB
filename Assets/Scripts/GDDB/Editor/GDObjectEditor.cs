using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GDDB.Editor
{
    [CustomEditor( typeof(GDObject), true )]
    public class GDObjectEditor : UnityEditor.Editor
    {
        private GDObject _target;
        private Int32 _lastSelectedComponentIndex = -1;

        protected virtual void OnEnable( )
        {
            _target = (GDObject)target;
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
            
            //Draw GDComponents list
            for ( int i = 0; i < compsProp.arraySize; i++ )
            {
                DrawComponentHeader( compsProp, i );

                var compProp = compsProp.GetArrayElementAtIndex( i );
                var compEndProp = compProp.GetEndProperty();
                if ( compProp.NextVisible( true ) )
                {
                    do
                    {
                        EditorGUILayout.PropertyField( compProp );
                    }   
                    while ( compProp.NextVisible( false ) && !SerializedProperty.EqualContents( compProp, compEndProp ) );
                }
            }

            so.ApplyModifiedProperties();


            DrawDelimiter();
            DrawComponentAddBtn();
        }

        private void DrawDelimiter( )
        {
            GUILayout.Space( 5 );
            GUILayout.Box( "", GUILayout.ExpandWidth( true ), GUILayout.Height( 1 ) );
        }

        private void DrawComponentHeader( SerializedProperty components, Int32 componentIndex )
        {
            var compProp = components.GetArrayElementAtIndex( componentIndex );

            GUILayout.BeginHorizontal(  );
            var compType          = compProp.managedReferenceValue.GetType();
            var componentTypeName = compProp.managedReferenceFullTypename;
            componentTypeName = !String.IsNullOrEmpty(componentTypeName) ? componentTypeName.Split('.').Last() : "Null";
            GUILayout.Label( componentTypeName, Styles.BoldLabel );
            GUILayout.FlexibleSpace();
            GUILayout.Label( $"({compType.FullName}, {compType.Assembly.GetName().Name})", Styles.ItalicLabel );
            if( GUILayout.Button( "X" ) )
            {
                RemoveComponent( componentIndex );
            }
            GUILayout.EndHorizontal();
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

        private static class Styles
        {
            public static readonly GUIStyle ItalicLabel = new ( EditorStyles.label ) { fontStyle = FontStyle.Italic };
            public static readonly GUIStyle BoldLabel   = EditorStyles.boldLabel;
        }
    }
}