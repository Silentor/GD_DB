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
using UnityEngine.Serialization;
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

        //Any changes in edited GDObject
        public static event Action<GDObject> Changed;

        private GDObject        _target;
        private VisualElement   _componentsContainer;
        private Settings        _settings;
        private List<Type>      _favorites;
        private VisualElement   _root;

        protected virtual void OnEnable( )
        {
            _target    = (GDObject)target;
            _settings  = new Settings();
            _favorites = new List<Type>();
            _settings.LoadFavoriteComponents( _favorites );

            GDAssets.GDDBAssetsChanged.Subscribe( OnGDDBChanged);
        }

        private void OnGDDBChanged(IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> removedObjects )
        {
            //if ( changedObjects.Contains( _target ) ) 
                //Repaint();
        }

        protected virtual void OnDisable( )
        {
            GDAssets.GDDBAssetsChanged.Unsubscribe( OnGDDBChanged );
        }

        public override VisualElement CreateInspectorGUI( )
        {
            _root = new VisualElement();
            _root.name = "GDObjectEditorRoot";
            _root.Add( CreateAll() );
            return _root;
        }

        public VisualElement CreateAll( )
        {
            var gdoVisualTreeAsset = Resources.GDObjectEditorAsset;
            var   gdoVisualTree      = gdoVisualTreeAsset.Instantiate();
            gdoVisualTree.TrackSerializedObjectValue( serializedObject, SerializedObjectChangedCallback );

            //Enabled toggle
            var enabledTgl  = gdoVisualTree.Q<Toggle>( "Enabled" );
            var enabledProp = serializedObject.FindProperty( nameof(GDObject.EnabledObject) );
            enabledTgl.BindProperty( enabledProp );

            //GD Object name field with renaming support
            var gdoName = gdoVisualTree.Q<TextField>( "Name" );
            gdoName.value = _target.name;
            gdoName.RegisterCallback<ChangeEvent<String>>( ( newValue ) => GDOName_Changed( gdoName, newValue ) );
            gdoName.TrackSerializedObjectValue( serializedObject, _ =>
            {
                if ( _target.name != gdoName.value )
                    gdoName.SetValueWithoutNotify( _target.name );
            });                    
            
            var flagsLbl = gdoVisualTree.Q<Label>( "Flags" );
            flagsLbl.schedule.Execute( () => Flags_MarkGDOBjectChangedState( flagsLbl ) ).Every( 100 );
            Flags_MarkGDOBjectChangedState( flagsLbl );

            //GD Object guid label
            var guid = gdoVisualTree.Q<Label>( "Guid" );
            guid.text = _target.Guid.ToString();

            //Disabled GD Object script reference field
            var script = gdoVisualTree.Q<ObjectField>( "Script" );
            script.SetEnabled( false );
            var scriptProp   = serializedObject.FindProperty( "m_Script" );
            script.BindProperty( scriptProp );
        
            //GD Object custom properties
            var properties   = gdoVisualTree.Q<VisualElement>( "Properties" );
            var compsProp    = serializedObject.FindProperty( nameof(GDObject.Components) );
            
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

            //Component list widgets
            _componentsContainer = gdoVisualTree.Q<VisualElement>( "Components" );
            CreateComponents( compsProp );
            _componentsContainer.TrackPropertyValue( compsProp, _ =>
            {
                Debug.Log( $"Changed Components prop" );
                RecreateComponents();
            } );

            //New component add button
            CreateAddComponentButton( compsProp, gdoVisualTree );

            ProcessDebugToolbar( gdoVisualTree );

            return gdoVisualTree;
        }

        private void RecreateAll( )
        {
            _root.Clear();
            _root.Add( CreateAll() );
        }

        private void CreateComponents( SerializedProperty componentsProp )
        {
            var hasMissedTypes = SerializationUtility.HasManagedReferencesWithMissingTypes( _target );
            if ( hasMissedTypes && EditorUtility.IsDirty( _target ) )
            {
                Debug.Log( $"[{nameof(GDObjectEditor)}]-[{nameof(CreateComponents)}] Missing reference types was detected in GDObject {_target.name}. Changes in GDObject was saved to disk. To handle missing Component type editor needs to read raw serialized data from asset file", _target );
                AssetDatabase.SaveAssetIfDirty( _target );      //We need be sure that in memory and on disk object is same (because we will use direct file reading)
            }

            for ( var i = 0; i < componentsProp.arraySize; i++ )
            {
                _componentsContainer.Add( CreateComponentGUI( componentsProp, componentsProp.GetArrayElementAtIndex( i ), i, hasMissedTypes ) );
            }
        }

        private void RecreateComponents( )
        {
            _componentsContainer.Clear();
            var componentsProp = serializedObject.FindProperty( nameof(GDObject.Components) );
            CreateComponents( componentsProp );
        }

        
        private VisualElement CreateComponentGUI( SerializedProperty componentsProp, SerializedProperty componentProp, Int32 index, Boolean hasMissingTypes )
        {
            VisualElement result  ;
            if ( componentProp.managedReferenceValue != null )                        //Component present, show default component widget
            {
                result = Resources.GDComponentEditorAsset.Instantiate();

                //Draw header
                var compType  = componentProp.managedReferenceValue.GetType();
                var typeFoldout = result.Q<Foldout>( "Type" );
                typeFoldout.text = compType.Name;
                
                var typeInfoLabel = result.Q<Label>( "TypeInfo" );
                typeInfoLabel.text = $"{compType.Assembly.GetName().Name} : {compType.Namespace}";

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
            else          //Missed or null component, show error widget
            {
                result = Resources.GDComponentEditorAsset.Instantiate();

                var missedTypeWidget = result.Q<VisualElement>( "MissedType" );
                missedTypeWidget.style.display = DisplayStyle.Flex;

                var typeFoldout = result.Q<Foldout>( "Type" );
                typeFoldout.AddToClassList( "component__type--error" );
                typeFoldout.RemoveFromClassList( "component__type" );

                var typeInfoLabel    = result.Q<Label>( "TypeInfo" );
                typeInfoLabel.AddToClassList( "component__type--error" );
                var descriptionLabel = result.Q<Label>( "ErrorDescription" );

                if ( !hasMissingTypes )         //Definitely null ref
                {
                    typeFoldout.text   = "Component reference is null";
                    typeInfoLabel.text = String.Empty;
                    descriptionLabel.text = "Null components is not supported. Use fix buttons to REMOVE null component type from this object or in entire database.";

                    var fixOnceBtn = result.Q<Button>( "FixOnce" );
                    fixOnceBtn.text = "Remove once";
                    fixOnceBtn.tooltip = "Remove this null component from this object";
                    fixOnceBtn.clicked += () => RemoveComponentByIndex( componentsProp, index );

                    var fixEverywhereBtn = result.Q<Button>( "FixEverywhere" );
                    fixEverywhereBtn.text = "Remove everywhere";
                    fixEverywhereBtn.tooltip = "Remove all null components from all objects in database";
                    fixEverywhereBtn.clicked += RemoveNullComponentsEverywhere;

                    Debug.Log( $"[{nameof(GDObjectEditor)}]-[{nameof(CreateComponentGUI)}] created component editor {index} for NULL component type" );
                }
                else              //Definitely missed ref
                {
                    var patcher          = new GDAssetPatcher( _target );
                    var compTypeFromFile = patcher.GetComponentType( index );

                    typeFoldout.text   = compTypeFromFile.Type;
                    typeInfoLabel.text = $"{compTypeFromFile.Assembly} : {compTypeFromFile.Namespace}";
                    descriptionLabel.text = "Component type not found in project. It may have been renamed or moved to another namespace or assembly. Use fix buttons to replace missed component type with existing type. Is cause direct asset file modification! Attention, this operation is no undoable.";

                    var fixOnceBtn = result.Q<Button>( "FixOnce" );
                    fixOnceBtn.clicked += () => FixComponentOnce( fixOnceBtn,  index, patcher );

                    var fixEverywhereBtn = result.Q<Button>( "FixEverywhere" );
                    fixEverywhereBtn.clicked += () => FixComponentEverywhere( fixOnceBtn,  index, patcher );

                    Debug.Log( $"[{nameof(GDObjectEditor)}]-[{nameof(CreateComponentGUI)}] created component editor {index} for missed component type {compTypeFromFile}" );
                }

                var removeBtn = result.Q<Button>( "Remove" );
                removeBtn.clicked += () => RemoveComponentByIndex( componentsProp, index );
            }

            return result;
        }

        private void CreateAddComponentButton( SerializedProperty componentsProp, VisualElement gdObjectVisualTree)
        {
            var addComponentBtn = gdObjectVisualTree.Q<Button>( "AddComponentBtn" );
            addComponentBtn.clicked += ( ) =>
            {
                ShowSelectComponentPopup( addComponentBtn, ( componentType ) => AddComponent( componentsProp, componentType ) );
            };
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
            Debug.Log( $"[{nameof(GDObjectEditor)}]-[{nameof(SerializedObjectChangedCallback)}] changed" );

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
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponent( SerializedProperty components, Int64 id )
        {
            for ( int i = 0; i < components.arraySize; i++ )
            {
                if ( components.GetArrayElementAtIndex( i ).managedReferenceId == id )
                {
                    components.DeleteArrayElementAtIndex( i );
                    //RemoveComponentGUI( i );
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void RemoveComponentByIndex( SerializedProperty components, Int32 index )
        {
            if ( index >= 0 && index < components.arraySize )
            {
                Debug.Log( $"[{nameof(GDObjectEditor)}]-[{nameof(RemoveComponentByIndex)}] removing component index {index}" );
                components.DeleteArrayElementAtIndex( index );
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void RemoveNullComponentsEverywhere( )
        {
            AssetDatabase.SaveAssets();
            var counter = 0;

            try
            {
                foreach ( var gdObject in GDBEditor.AllObjects )
                {
                    if ( gdObject.Components.Any( c => c is null ) )            //Something wrong here (null ref component of missed component type)
                    {
                        var isChangedObject = false;
                        var result          = false;
                        do
                        {
                            result = FindAndRemoveNullComponent( gdObject );
                            isChangedObject |= result;
                        } while ( result );

                        if ( isChangedObject )
                        {
                            counter++;
                        }
                    }

                    if( EditorUtility.DisplayCancelableProgressBar( "GDDB component fix", "Remove null components", counter / (Single)GDBEditor.AllObjects.Count ) )
                        break;
                }
            }
            finally
            {
                Debug.Log( $"[{nameof(GDObjectEditor)}]-[{nameof(RemoveNullComponentsEverywhere)}] Removed all null components from {counter} GDObjects" );
                EditorUtility.ClearProgressBar();    
            }

            Boolean FindAndRemoveNullComponent(GDObject gdObject )
            {
                var isChanged      = false;
                var patcher        = new GDAssetPatcher( gdObject );
                var nullRefIdIndex = patcher.ComponentIds.FindIndex( cid => cid == ManagedReferenceUtility.RefIdNull );
                if ( nullRefIdIndex >= 0 )
                {
                    var componentsProp = new SerializedObject( gdObject ).FindProperty( nameof(GDObject.Components) );
                    componentsProp.DeleteArrayElementAtIndex( nullRefIdIndex );
                    isChanged = componentsProp.serializedObject.ApplyModifiedProperties();
                }

                return isChanged;
            }
        }

        private void FixComponentOnce( Button button, Int32 componentIndex, GDAssetPatcher objectPatcher)
        {
            //Show select component popup for select component to fix missed
            ShowSelectComponentPopup( button, ( componentType ) =>
            {
                objectPatcher.ReplaceComponentType( componentIndex, new GDAssetPatcher.ComponentType( componentType ) );
                serializedObject.Update();
            } );
        }

        private void FixComponentEverywhere( Button button, Int32 componentIndex, GDAssetPatcher objectPatcher)
        {
            //Show select component popup for select component to fix missed
            ShowSelectComponentPopup( button, ( newType ) =>
            {
                var oldType = objectPatcher.GetComponentType( componentIndex );
                GDAssetPatcher.ReplaceComponentTypeEverywhere( oldType, new GDAssetPatcher.ComponentType( newType ) );
                serializedObject.Update();
            } );
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
            return  EditorPrefs.GetBool( GetComponentFoldoutKey(componentType), true );
        }

        private void SetComponentFoldout( VisualElement propertiesContainer, Type componentType, Boolean state )
        {
            propertiesContainer.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            EditorPrefs.SetBool( GetComponentFoldoutKey(componentType), state );
        }

        private static String GetComponentFoldoutKey( Type componentType )
        {
            return $"{Application.identifier}.{componentType.Name}_Foldout";
        }

        private void ShowSelectComponentPopup( Button sender, Action<Type> onSelectComponent )
        {
            var searchPopupLogic = new SearchPopup( _settings );
            searchPopupLogic.Selected += onSelectComponent;
            searchPopupLogic.Closed   += ( ) => _settings.LoadFavoriteComponents( _favorites );
            PopupWindow.Show( sender.worldBound, searchPopupLogic );
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

            // var debugReferenceBtn = new Button( ( ) => {
            //     _target.DebugReference = _target.Components.FirstOrDefault();
            // } );
            // debugReferenceBtn.text         = "Debug ref";
            // debugReferenceBtn.style.width  = 100;
            // debugReferenceBtn.style.height = 20;
            // toolbar.Add( debugReferenceBtn );
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