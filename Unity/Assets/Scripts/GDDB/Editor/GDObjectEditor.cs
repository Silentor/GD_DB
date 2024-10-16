using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GDDB.Serialization;
using JetBrains.Annotations;
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
        private Settings        _settings;
        private List<Type>      _favorites;

        protected virtual void OnEnable( )
        {
            _target   = (GDObject)target;
            _settings = new Settings();
            LoadFavoriteComponents();
        }

        protected virtual void OnDisable( )
        {
        }

        public override VisualElement CreateInspectorGUI( )
        {
            var gdoVisualTreeAsset = Resources.GDObjectEditorAsset;
            var   gdoVisualTree      = gdoVisualTreeAsset.Instantiate();
            gdoVisualTree.TrackSerializedObjectValue( serializedObject, SerializedObjectChangedCallback );

            //Enabled toggle
            var enabledTgl = gdoVisualTree.Q<Toggle>( "Enabled" );
            enabledTgl.value = _target.EnabledObject;
            enabledTgl.RegisterValueChangedCallback( EnabledToggle_Changed );

            //GD Object name field with renaming support
            var gdoName = gdoVisualTree.Q<TextField>( "Name" );
            gdoName.value = _target.name;
            gdoName.RegisterCallback<ChangeEvent<String>>( ( newValue ) => GDOName_Changed( gdoName, newValue ) );
            
            var flagsLbl = gdoVisualTree.Q<Label>( "Flags" );
            flagsLbl.TrackSerializedObjectValue( serializedObject, _ => Flags_MarkGDOBjectChangedState( flagsLbl ) );
            Flags_MarkGDOBjectChangedState( flagsLbl );

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

            ProcessDebugToolbar( gdoVisualTree );

            return gdoVisualTree;
        }

        
        private void CreateComponentGUI( SerializedProperty componentsProp, SerializedProperty componentProp )
        {
            VisualElement result  ;
            if ( componentProp.managedReferenceValue != null )
            {
                result = Resources.GDComponentEditorAsset.Instantiate();

                //Draw header
                var compType  = componentProp.managedReferenceValue.GetType();
                var typeFoldout = result.Q<Foldout>( "Type" );
                typeFoldout.text = compType.Name;
                
                var typeInfoLabel = result.Q<Label>( "TypeInfo" );
                typeInfoLabel.text = $"({compType.FullName}, {compType.Assembly.GetName().Name})";

                var removeBtn     = result.Q<Button>( "Remove" );
                var catchId       = componentProp.managedReferenceId;
                removeBtn.clicked += () => RemoveComponent( componentsProp, catchId );

                var scriptIcon = result.Q<Button>( "ScriptIcon" );
                scriptIcon.style.backgroundImage =  _favorites.Contains( compType ) ? Resources.FavoriteIcon : Resources.CSharpIcon;
                scriptIcon.clicked               += ( ) => ComponentIconClicked( scriptIcon, compType ); 

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
                result = Resources.GDComponentEditorAsset.Instantiate();

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
                var searchPopupLogic = new SearchPopup( this, componentsProp, _settings );
                searchPopupLogic.Closed += LoadFavoriteComponents;
                PopupWindow.Show( addComponentBtn.worldBound, searchPopupLogic );
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

        private void Flags_MarkGDOBjectChangedState( Label sender )
        {
            if ( EditorUtility.IsDirty( _target ) )
                sender.text = "*";
            else
                sender.text = String.Empty;
        }

        private void SerializedObjectChangedCallback( SerializedObject target )
        {
            Changed?.Invoke( _target );
        }

        public void AddComponent( SerializedProperty components, [NotNull] Type componentType )
        {
            if ( componentType == null ) throw new ArgumentNullException( nameof(componentType) );
            if( !componentType.IsSubclassOf( typeof(GDComponent) ) )
                throw new ArgumentException( $"Type {componentType.FullName} must be inherited from GDComponent" );
            if( componentType.IsAbstract )
                throw new ArgumentException( $"Type {componentType.FullName} must be not abstract" );

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

        private void ComponentIconClicked( Button iconButton, Type componentType )
        {
            if ( _favorites.Contains( componentType ) )
            {
                _favorites.Remove( componentType );
                iconButton.style.backgroundImage = Resources.CSharpIcon;
            }
            else
            {
                _favorites.Add( componentType );
                iconButton.style.backgroundImage = Resources.FavoriteIcon;
            }

            _settings.SaveFavoriteComponents( _favorites );
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

        private void LoadFavoriteComponents( )
        {
            _favorites = new List<Type>();
            _settings.LoadFavoriteComponents( _favorites );
        }

        private void ProcessDebugToolbar( VisualElement root )
        {
            var toolbar = new Box()
                          {
                                  style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween }
                          };
            root.Add( toolbar );

            //Save db to json
            var toJsonBtn = new Button( ( ) => 
            {
                var editorDB        = GDBEditor.GDB;
                var assetsReference = CreateInstance<DirectAssetReferences>();
                var serializer      = new DBJsonSerializer();
                var jsonStr         = serializer.Serialize( editorDB.RootFolder, editorDB.AllObjects, assetsReference );

                //Save hierarchy to json
                var dbOutputPath   = Application.streamingAssetsPath + $"/{editorDB.Name}.gddb.json";
                using var dbFile = File.Create( dbOutputPath );
                using var writer = new StreamWriter( dbFile );
                writer.Write( jsonStr );
                writer.Flush();
                dbFile.Flush();

                //Save referenced assets to scriptable object list
                var assetsPath = $"Assets/Resources/{editorDB.Name}.assets.asset";
                AssetDatabase.CreateAsset( assetsReference, assetsPath );

                AssetDatabase.Refresh();

                Debug.Log( $"[{nameof(GDObjectEditor)}] Saved gddb to {dbOutputPath}, size {EditorUtility.FormatBytes( dbFile.Length)}, asset resolver to {assetsPath}" );
            } );
            toJsonBtn.text         = $"DB to Json ({GDBEditor.GDB.Name}.gddb.json)";
            toJsonBtn.style.width  = 100;
            toJsonBtn.style.height = 20;
            toolbar.Add( toJsonBtn );

            //Save db to scriptable object
            var toSoBtn = new Button( ( ) => {
                var serializer = new DBAssetSerializer();
                var dbAsset = serializer.Serialize( GDBEditor.GDB.RootFolder);
                AssetDatabase.CreateAsset( dbAsset, $"Assets/Resources/{GDBEditor.GDB.Name}.folders.asset" );
            } );
            toSoBtn.text       = "DB to SO";
            toSoBtn.style.width  = 100;
            toSoBtn.style.height = 20;
            toolbar.Add( toSoBtn );

            //Load bd from json and print to console 
            var fromJsonBtn = new Button( ( ) => {
                var path = EditorUtility.OpenFilePanel( "Open JSON", Application.streamingAssetsPath, "json" );
                if ( !String.IsNullOrEmpty( path ) )
                {
                    var textFromFile = File.ReadAllText( path );
                    var gdJson = new GdJsonLoader( textFromFile ).GetGameDataBase();
                    Debug.Log( $"Loaded gd DB {gdJson.Name}, total objects {gdJson.AllObjects.Count}" );
                    gdJson.Print();
                }
            } );
            fromJsonBtn.text           = "Print Json DB";
            fromJsonBtn.style.width  = 100;
            fromJsonBtn.style.height = 20;
            toolbar.Add( fromJsonBtn );

            //Print bd from editor to console
            var printHierarchy = new Button( ( ) => 
            {
                var gddb = new GdEditorLoader(  ).GetGameDataBase();
                gddb.Print();
            } );
            printHierarchy.text         = "Print editor DB";
            printHierarchy.style.width  = 100;
            printHierarchy.style.height = 20;
            toolbar.Add( printHierarchy );

            var printObjBtn = new Button( ( ) => {
                var json = new ObjectsJsonSerializer().Serialize( new []{ _target } );
                Debug.Log( json );
            } );
            printObjBtn.text         = "Print obj";
            printObjBtn.style.width  = 100;
            printObjBtn.style.height = 20;
            toolbar.Add( printObjBtn );
        }

        private static class Resources
        {
            public static VisualTreeAsset GDObjectEditorAsset = UnityEngine.Resources.Load<VisualTreeAsset>( "GDObjectEditor" );
            public static VisualTreeAsset GDComponentEditorAsset = UnityEngine.Resources.Load<VisualTreeAsset>( "GDComponentEditor" );

            public static Texture2D CSharpIcon    = UnityEngine.Resources.Load<Texture2D>( "tag_24dp" );
            public static Texture2D FavoriteIcon  = UnityEngine.Resources.Load<Texture2D>( "star_24dp" );
        }
    }
}