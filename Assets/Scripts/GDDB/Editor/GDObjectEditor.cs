﻿using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace GDDB.Editor
{
    [CustomEditor( typeof(GDObject), true )]
    public class GDObjectEditor : UnityEditor.Editor
    {
        private GDObject        _target;
        private Int32           _lastSelectedComponentIndex = -1;
        private Type[]          _allProperComponents;
        private VisualElement _componentsContainer;
        private VisualTreeAsset _gdcVisualTreeAsset;
        private VisualTreeAsset _gdoVisualTreeAsset;

        protected virtual void OnEnable( )
        {
            _target = (GDObject)target;
        }

        protected virtual void OnDisable( )
        {
        }

        public override VisualElement CreateInspectorGUI( )
        {
            var gdoVisualTreeAsset = Resources.Load<VisualTreeAsset>(("GDObjectEditor"));
            var   gdoVisualTree      = gdoVisualTreeAsset.Instantiate();
        
            //GD Object name field with renaming support
            var gdoName = gdoVisualTree.Q<TextField>( "Name" );
            gdoName.value = _target.name;
            gdoName.RegisterCallback<ChangeEvent<String>>( GDOName_Changed );
            
            //GD Object guid label
            var guid = gdoVisualTree.Q<Label>( "Guid" );
            guid.text = _target.Guid.ToString();

            //Disabled GD Object script reference field
            var script = gdoVisualTree.Q<PropertyField>( "Script" );
            script.SetEnabled( false );
        
            //GD Object custom properties
            var properties   = gdoVisualTree.Q<VisualElement>( "Properties" );
            var scriptProp   = serializedObject.FindProperty( "m_Script" );
            var compsProp    = serializedObject.FindProperty( "Components" );
            var enabledProp    = serializedObject.FindProperty( "Enabled" );
            var gdObjectProp = serializedObject.GetIterator();
            for (var enterChildren = true; gdObjectProp.NextVisible(enterChildren); enterChildren = false)
            {
                //Hide completely, custom draw
                if ( SerializedProperty.EqualContents( gdObjectProp, compsProp) || SerializedProperty.EqualContents( gdObjectProp, scriptProp ) 
                  || SerializedProperty.EqualContents( gdObjectProp, enabledProp ) )
                    continue;
            
                //Draw GDObject properties
                properties.Add( new PropertyField( gdObjectProp ) );
            }

            //Components widgets
            _componentsContainer = gdoVisualTree.Q<VisualElement>( "Components" );
            for ( var i = 0; i < compsProp.arraySize; i++ )
            {
                CreateComponentGUI( compsProp , i );
            }

            //New component add button
            CreateComponentAddButton( compsProp, gdoVisualTree );

            gdoVisualTree.Bind( serializedObject );
            return gdoVisualTree;
        }

        private void CreateComponentGUI( SerializedProperty componentsProp, Int32 index )
        {
            var componentProp = componentsProp.GetArrayElementAtIndex( index );
            VisualElement result  ;
            if ( componentProp.managedReferenceValue != null )
            {
                result = GetGDComponentEditorTemplate().Instantiate();

                //Draw header
                var compType  = componentProp.managedReferenceValue.GetType();
                var typeFoldout = result.Q<Foldout>( "Type" );
                typeFoldout.text = compType.Name;
                
                var typeInfoLabel = result.Q<Label>( "TypeInfo" );
                typeInfoLabel.text = $"({compType.FullName}, {compType.Assembly.GetName().Name})";

                var removeBtn     = result.Q<Button>( "Remove" );
                removeBtn.clicked += () => RemoveComponent( componentsProp , index );

                //Draw body
                var propertiesContainer = result.Q<VisualElement>( "Properties" );
                var compEndProp         = componentProp.GetEndProperty();
                if ( componentProp.NextVisible( true ) && !SerializedProperty.EqualContents( componentProp, compEndProp ) )
                {
                    do
                    {
                        propertiesContainer.Add( new PropertyField( componentProp ) );
                    }   
                    while ( componentProp.NextVisible( false ) && !SerializedProperty.EqualContents( componentProp, compEndProp ) );
                }
                typeFoldout.SetValueWithoutNotify( IsComponentFoldout( compType ) );
                SetComponentFoldout( propertiesContainer, compType, typeFoldout.value );
                typeFoldout.RegisterValueChangedCallback( evt => SetComponentFoldout( propertiesContainer, compType, evt.newValue ) );
            }
            else
            {
                result = GetGDComponentEditorTemplate().Instantiate();

                var typeFoldout = result.Q<Foldout>( "Type" );
                typeFoldout.AddToClassList( "component__type--error" );
                typeFoldout.RemoveFromClassList( "component__type" );

                if( String.IsNullOrEmpty( componentProp.managedReferenceFullTypename) )
                    typeFoldout.text = "Component type cannot be found, look at the asset file";
                else
                    typeFoldout.text = "Component somehow is null";

                var removeBtn = result.Q<Button>( "Remove" );
                removeBtn.clicked += () => RemoveComponent( componentsProp , index );
            }

            _componentsContainer.Add( result );
        }

        private void RemoveComponentGUI( Int32 index )
        {
            _componentsContainer.RemoveAt( index );
        }

        private void CreateComponentAddButton( SerializedProperty componentsProp, VisualElement gdObjectVisualTree)
        {
            var allComponentTypes   = TypeCache.GetTypesDerivedFrom( typeof(GDComponent) );
            _allProperComponents = allComponentTypes.Where( t => !t.IsAbstract ).OrderBy( t => t.Name ).ToArray();
            var allProperNames      = _allProperComponents.Select( t => t.Name ).ToList();

            var componentsList = gdObjectVisualTree.Q<DropdownField>( "ComponentsList" );
            componentsList.choices = allProperNames;
            componentsList.RegisterValueChangedCallback( ComponentsList_Changed );

            var addComponentBtn = gdObjectVisualTree.Q<Button>( "AddComponentBtn" );
            addComponentBtn.clicked += ( ) =>
            {
                if ( _lastSelectedComponentIndex >= 0 && _lastSelectedComponentIndex < _allProperComponents.Length )
                {
                    var typeToAdd = _allProperComponents[ _lastSelectedComponentIndex ];
                    AddComponent( componentsProp, typeToAdd );
                }
            };
        }

        private void ComponentsList_Changed( ChangeEvent<String> evt )
        {
            _lastSelectedComponentIndex = ((DropdownField)evt.target).index;
        }

        private void GDOName_Changed( ChangeEvent<String> newName )
        {
            if( newName.newValue != _target.name )
                AssetDatabase.RenameAsset( AssetDatabase.GetAssetPath( _target ), newName.newValue );
        }

        private void AddComponent( SerializedProperty components, Type componentType )
        {
            var newComponent = Activator.CreateInstance( componentType );

            var lastIndex = components.arraySize;
            components.InsertArrayElementAtIndex( lastIndex );
            components.GetArrayElementAtIndex( lastIndex ).managedReferenceValue = newComponent;
            CreateComponentGUI( components, lastIndex );

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponent( SerializedProperty components, Int32 componentIndex )
        {
            components.DeleteArrayElementAtIndex( componentIndex );
            RemoveComponentGUI( componentIndex );

            serializedObject.ApplyModifiedProperties();
        }

        private Boolean IsComponentFoldout( Type componentType )
        {
            return  EditorPrefs.GetBool( componentType.Name, true );
        }

        private void SetComponentFoldout( VisualElement propertiesContainer, Type componentType, Boolean state )
        {
            propertiesContainer.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            EditorPrefs.SetBool( componentType.Name, state );
        }

        private VisualTreeAsset GetGDOObjectEditorTemplate( )
        {
            if( _gdoVisualTreeAsset == null )
                _gdoVisualTreeAsset = Resources.Load<VisualTreeAsset>(("GDObjectEditor"));
            return _gdoVisualTreeAsset;
        }

        private VisualTreeAsset GetGDComponentEditorTemplate( )
        {
            if( _gdcVisualTreeAsset == null )
                _gdcVisualTreeAsset = Resources.Load<VisualTreeAsset>(("GDComponentEditor"));
            return _gdcVisualTreeAsset;
        }
    }
}