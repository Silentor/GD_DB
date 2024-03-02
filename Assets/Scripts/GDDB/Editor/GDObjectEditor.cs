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

        private void OnEnable( )
        {
            _target = (GDObject)target;
        }

        public override void OnInspectorGUI( )
        {
            //base.OnInspectorGUI();

            var so        = serializedObject;
            so.UpdateIfRequiredOrScript();

            var compsProp = so.FindProperty( "Components" );

            //Draw properties of GDObject descendants
            var gdObjectProp = so.GetIterator();
            for (var enterChildren = true; gdObjectProp.NextVisible(enterChildren); enterChildren = false)
            {
                //Hide completely, custom draw
                if ( SerializedProperty.EqualContents( gdObjectProp, compsProp) )
                    continue;

                using (new EditorGUI.DisabledScope("m_Script" == gdObjectProp.propertyPath))
                    EditorGUILayout.PropertyField(gdObjectProp, true);
            }
            
            //Draw GDComponents list
            for ( int i = 0; i < compsProp.arraySize; i++ )
            {
                DrawComponentHeader( compsProp, i );

                var compProp = compsProp.GetArrayElementAtIndex( i );
                var compEndProp = compProp.GetEndProperty();
                while (compProp.NextVisible(true) && !SerializedProperty.EqualContents(compProp, compEndProp))
                {
                    EditorGUILayout.PropertyField( compProp );
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
            var componentTypeName = compProp.managedReferenceFullTypename;
            componentTypeName = !String.IsNullOrEmpty(componentTypeName) ? componentTypeName.Split('.').Last() : "Null";
            GUILayout.Label( componentTypeName, EditorStyles.boldLabel );
            GUILayout.FlexibleSpace();
            if( GUILayout.Button( "X" ) )
            {
                components.DeleteArrayElementAtIndex( componentIndex );
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
                        var newComponent = Activator.CreateInstance( allProperComponents[_lastSelectedComponentIndex] );
                        _target.Components.Add( (GDComponent)newComponent );
                    }
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}