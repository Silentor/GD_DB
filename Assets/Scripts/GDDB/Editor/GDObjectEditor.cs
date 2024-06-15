using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;
using PopupWindow = UnityEditor.PopupWindow;
using Random = UnityEngine.Random;

namespace GDDB.Editor
{
    [CustomEditor( typeof(GDObject), true )]
    public class GDObjectEditor : UnityEditor.Editor
    {
        public VisualElement Components => _componentsContainer;

        public static event Action<GDObject> Changed;

        private GDObject        _target;
        private VisualElement   _componentsContainer;
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
        
            //Enabled toggle
            var enabledTgl = gdoVisualTree.Q<Toggle>( "Enabled" );
            enabledTgl.value = _target.EnabledObject;
            enabledTgl.RegisterValueChangedCallback( EnabledToggle_Changed );

            //GD Object name field with renaming support
            var gdoName = gdoVisualTree.Q<TextField>( "Name" );
            gdoName.value = _target.name;
            gdoName.RegisterCallback<ChangeEvent<String>>( ( newValue ) => GDOName_Changed( gdoName, newValue ) );
            
            var flagsLbl = gdoVisualTree.Q<Label>( "Flags" );
            flagsLbl.TrackSerializedObjectValue( serializedObject, (so) => Flags_SerializedObjectChangedCallback( flagsLbl, so ) );
            Flags_SerializedObjectChangedCallback( flagsLbl, serializedObject );

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
                CreateComponentGUI( compsProp , compsProp.GetArrayElementAtIndex( i ) );
            }

            //New component add button
            CreateComponentAddButton( compsProp, gdoVisualTree );

            //Debug write to json button
            var debugToolbar = new Box()
                          {
                                  style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween }
                          };
            gdoVisualTree.Add( debugToolbar );

            var toJsonBtn = new Button( ( ) => {
                var json = new GDJson().GDToJson( new []{ _target } );
                Debug.Log( json );
            } );
            toJsonBtn.text         = "To Json";
            toJsonBtn.style.width  = 100;
            toJsonBtn.style.height = 20;
            debugToolbar.Add( toJsonBtn );

            return gdoVisualTree;
        }

        
        private void CreateComponentGUI( SerializedProperty componentsProp, SerializedProperty componentProp )
        {
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
                var catchId       = componentProp.managedReferenceId;
                removeBtn.clicked += () => RemoveComponent( componentsProp, catchId );

                //Draw body
                var propertiesContainer = result.Q<VisualElement>( "Properties" );
                var compEndProp         = componentProp.GetEndProperty();
                if ( componentProp.NextVisible( true ) && !SerializedProperty.EqualContents( componentProp, compEndProp ) )
                {
                    do
                    {
                        var propertyField = new PropertyField( componentProp );
                        propertyField.BindProperty( componentProp );                    //Because we can add component ui after editor creation, so auto bind not working
                        propertiesContainer.Add( propertyField );
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
                var catchId   = componentProp.managedReferenceId;
                removeBtn.clicked += () => RemoveComponent( componentsProp , catchId );
            }

            _componentsContainer.Add( result );
        }

        private void RemoveComponentGUI( Int32 index )
        {
            _componentsContainer.RemoveAt( index );
        }

        private void CreateComponentAddButton( SerializedProperty componentsProp, VisualElement gdObjectVisualTree)
        {
            var addComponentBtn = gdObjectVisualTree.Q<Button>( "AddComponentBtn" );
            addComponentBtn.clicked += ( ) =>
            {
                PopupWindow.Show( addComponentBtn.worldBound, new SearchPopup( this, componentsProp ) );
            };
        }             

        private void EnabledToggle_Changed(ChangeEvent<Boolean> evt )
        {
            serializedObject.Update();
            var enabledObjProp = serializedObject.FindProperty( nameof(GDObject.EnabledObject) );
            enabledObjProp.boolValue = evt.newValue;
            serializedObject.ApplyModifiedProperties();
        }

        private void GDOName_Changed( TextField sender, ChangeEvent<String> newName )
        {
            if ( newName.newValue != _target.name )
            {
                ObjectNames.SetNameSmart( _target, newName.newValue );
                if( _target.name != newName.newValue )
                    sender.SetValueWithoutNotify( _target.name );                           //Incorrect symbols in name
            }
        }

        private void Flags_SerializedObjectChangedCallback( Label sender, SerializedObject target )
        {
            if ( EditorUtility.IsDirty( _target ) )
                sender.text = "*";
            else
                sender.text = String.Empty;

            Changed?.Invoke( _target );
        }

        public void AddComponent( SerializedProperty components, Type componentType )
        {
            var newComponent = Activator.CreateInstance( componentType );

            var lastIndex = components.arraySize;
            components.InsertArrayElementAtIndex( lastIndex );
            var componentProp = components.GetArrayElementAtIndex( lastIndex );
            componentProp.managedReferenceValue = newComponent;
            CreateComponentGUI( components, componentProp );

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponent( SerializedProperty components, Int64 id )
        {
            for ( int index = 0; index < components.arraySize; index++ )
            {
                if ( components.GetArrayElementAtIndex( index ).managedReferenceId == id )
                {
                    components.DeleteArrayElementAtIndex( index );
                    RemoveComponentGUI( index );

                    serializedObject.ApplyModifiedProperties();
                }
            }
            
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