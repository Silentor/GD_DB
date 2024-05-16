using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdType) )]
    public class GdTypeDrawer : PropertyDrawer
    {
        //Because of UIToolkit we cannot cache visual elements bug https://forum.unity.com/threads/createpropertygui-called-multiple-times.1572943/
        //private          VisualElement              _root;
        //private          VisualElement              _gdType;
        //private          VisualElement              _toolbar;

        private          SerializedObject         _serializedObject;
        private          SerializedProperty       _dataProp;
        
        private readonly GDTypeHierarchy          _typeHierarchy;
        private readonly GDTypeHierarchy.Category _typesRoot;
        private readonly StyleSheet               _gdTypeStyles;
        
        private readonly GDObjectsFinder          _gdoFinder;

        private State _state;


        public GdTypeDrawer( )
        {
            _typeHierarchy = new GDTypeHierarchy();
            _typesRoot = _typeHierarchy.Root;
            _gdTypeStyles = UnityEngine.Resources.Load<StyleSheet>( "GDType" );
            _gdoFinder = new GDObjectsFinder();

            Debug.Log( $"Constructor" );
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            //base.OnGUI( position, property, label );
            var gdTypeProp = property.FindPropertyRelative( nameof(GdType.Data) );
            if ( _state == null )
                _state = CreateState( label.text, gdTypeProp );

            DrawStateIMGUI( position, gdTypeProp, _state );

        }

        private void DrawStateIMGUI( Rect position, SerializedProperty prop, State state )
        {
            //Draw label
            position = EditorGUI.PrefixLabel( position, new GUIContent( state.Label ) );

            //Draw Categories or value
            if( state.Value != null )
                GUI.Label( position, state.Value );
            if ( state.Categories.Count > 0 )
            {
                var categoryWidth = position.width / state.Categories.Count;
                var categoryPosition = new Rect( position.x, position.y, categoryWidth, position.height );
                for ( var i = 0; i < state.Categories.Count(); i++ )
                {
                    DrawCategoryIMGUI( categoryPosition, prop, state.Categories[ i ], i );
                    categoryPosition.x += categoryWidth;                    
                }
            }
            
        }

        private void DrawCategoryIMGUI( Rect categoryPosition, SerializedProperty prop, GDTypeHierarchy.Category category, Int32 index )
        {                 
            var gdType = new GdType( prop.intValue );
            var value = gdType[ index ];

            if ( category.Type == GDTypeHierarchy.CategoryType.Enum )
            {
                var names = category.Items.Select( i => i.Name ).ToArray();
                var valueIndex = category.Items.FindIndex( i => i.Value == value );
                EditorGUI.BeginChangeCheck();
                var newIndex = EditorGUI.Popup( categoryPosition, valueIndex, names );
                if ( EditorGUI.EndChangeCheck() )
                {
                    value = category.Items[ newIndex ].Value;
                    gdType[ index ] = value;
                    prop.intValue = (Int32)gdType.Data;
                    prop.serializedObject.ApplyModifiedProperties();
                    _state = null;
                }
            }
            else 
                EditorGUI.IntField( categoryPosition, value );
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            return null;    //Disable UIToolkit drawer

            Debug.Log( $"Create property gui" );

            _serializedObject = property.serializedObject;
            _dataProp         = property.FindPropertyRelative( nameof(GdType.Data) );

            var root      = new VisualElement();
            root.name = "Root";
            root.style.flexDirection = FlexDirection.Row;
            root.styleSheets.Add( _gdTypeStyles );

            var label = new Label( property.displayName );
            label.style.flexBasis = new StyleLength( new Length( 41, LengthUnit.Percent ) );
            label.style.minWidth = 130;
            root.Add( label );

            var gdType                     = new VisualElement();
            gdType.name                = "GDType";
            gdType.style.flexDirection = FlexDirection.Row;
            gdType.style.flexGrow      = 1;
            //_typeWidgetsContainer.AddToClassList( "unity-base-field__aligned" );        
            root.Add( gdType );
            //var label = new Label( property.displayName );
            //label.AddToClassList( "unity-property-field__label" );
            //label.AddToClassList( "unity-base-field__label" );
            //label.AddToClassList( "unity-object-field__label" );
            //container.Add( label );

            var toolbar = new VisualElement();
            toolbar.name = "Toolbar";
            toolbar.AddToClassList( "toolbar" );
            root.Add( toolbar );

            RecreateProperty( root );

            return  root;
        }

        private State CreateState( String label, SerializedProperty gdTypeProp )
        {
            var gdType = new GdType( gdTypeProp.intValue );
            if ( gdType == default )
                return CreateNoneTypeState( label, gdTypeProp );
            else
            {
                return CreateTypeState( label, gdTypeProp, gdType );
            }
        }

        private State CreateTypeState( String label, SerializedProperty gdTypeProp, GdType gdType )
        {
            return new State()
                   {
                           Label = label,
                           Type = gdType,
                           Categories =
                           {
                                   _typeHierarchy.Root,
                                   new GDTypeHierarchy.Category(){},
                                   new GDTypeHierarchy.Category(){},
                                   new GDTypeHierarchy.Category(){},
                           },
                           Buttons = new List<State.Button>()
                                     {
                                             new State.Button()
                                             {
                                                     Type = State.EButton.ClearType,
                                                     //Click = () => AssignType( root )
                                             }
                                     }
                   };
        }

        private State CreateNoneTypeState( String label, SerializedProperty gdTypeProp )
        {
            return new State()
            {
                Label = label,
                Value = "None",
                Buttons = new List<State.Button>()
                {
                    new State.Button()
                    {
                        Type = State.EButton.AssignType,
                        //Click = () => AssignType( root )
                    }
                }
            };
        }

        private void RecreateProperty( VisualElement root )
        {
            root.RemoveFromClassList( "duplicateType" );
            var gdType = GetGDTypeFromRoot( root );
            var toolbar = GetToolbarFromRoot( root );

            gdType.Clear();
            toolbar.Clear();

            if( _dataProp.intValue == 0 )
            {
                CreateNoneType( root );
            }
            else
            {
                CreateWidgets( root, 0, _typesRoot );

                var menuBtn = new Button( );
                menuBtn.clicked += () => OpenContextMenu( root );
                menuBtn.text                  = "";
                menuBtn.tabIndex              = 2;
                menuBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "menu_24dp" ) );
                menuBtn.tooltip               = "Open context menu";
                menuBtn.AddToClassList( "toolbar-square-button" );
                toolbar.Add( menuBtn );

                var clearTypeBtn = new Button( () => ClearType( root) );
                clearTypeBtn.text                  = "";
                clearTypeBtn.tabIndex              = 10;
                clearTypeBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "delete_forever_24dp" ) );
                clearTypeBtn.tooltip               = "Clear type";
                clearTypeBtn.AddToClassList( "toolbar-square-button" );
                toolbar.Add( clearTypeBtn );

                CheckDuplicateType( root );
            }
        }



        private void  CreateNoneType( VisualElement root )
        {
            var noneLabel = new Label("None");
            var gdType    = GetGDTypeFromRoot( root );
            gdType.Add( noneLabel );

            var addTypeBtn = new Button( () => AssignType(root) );
            addTypeBtn.text     = "";
            addTypeBtn.tabIndex = 1;
            addTypeBtn.AddToClassList( "toolbar-square-button" );
            addTypeBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "add_24dp" ) );
            addTypeBtn.tooltip = "Assign type";
            var toolbar = GetToolbarFromRoot( root );
            toolbar.Add( addTypeBtn );
        }

        private void CreateWidgets( VisualElement root, Int32 index, GDTypeHierarchy.Category category )
        {
            if( index >= 4 )
                return;

            var gdType = GetGDTypeFromRoot( root );
            //Debug.Log( $"Creating widget at {index} position" );

            //Clear this widget and all next
            while ( gdType.childCount > index )
                gdType.RemoveAt( index );

            var gdTypeValue = new GdType( _dataProp.intValue );
            var widget = CreateCategoryField( root, category, gdTypeValue[ index ], index );
            gdType.Add( widget );
            CheckIncorrectType( gdTypeValue, widget, index );

            //Create next widget(s)
            var nextCategory = category?.FindItem( gdTypeValue[ index ] )?.Subcategory;
            CreateWidgets( root, index + 1, nextCategory );
        }

        private VisualElement CreateCategoryField( VisualElement root, GDTypeHierarchy.Category category, Int32 value, Int32 index )
        {
            VisualElement result = null;
            if ( category == null )         //Default Int8 field
            {
                result                = new IntegerField( 3 ){ value = value };
                result.style.minWidth = 50;
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>>( OnChangeEnumWidget );

                Debug.Log( $"Created int field, index {index}, value {value}" );
            }
            else if ( category.Type == GDTypeHierarchy.CategoryType.Enum )
            {
                result = new PopupField<Int32>( GetValues( category, value ), value, i => ValueToString( category, i ),
                        i => ValueToString( category, i ) );
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>>( OnChangeEnumWidget );

                Debug.Log( $"Created popup field, index {index} for {category.UnderlyingType} category, value {category.FindItem( value ).Name}" );

                List<Int32> GetValues( GDTypeHierarchy.Category category, Int32 selectedItem )
                {
                    var validValues = category.Items.Select( i => i.Value ).ToList();
                    if( !validValues.Contains( selectedItem ) )
                        validValues.Add( selectedItem );
                    return validValues;
                }

                String ValueToString( GDTypeHierarchy.Category category, Int32 value )
                {
                    if( category.IsCorrectValue( value ) )
                        return category.FindItem( value ).Name;

                    return $"Incorrect value {value}";
                }
            }

            return result;

            //Rebuild widgets after change
            void OnChangeEnumWidget( ChangeEvent<Int32> evt )
            {
                _serializedObject.Update();
                var gdType = new GdType( _dataProp.intValue );
                gdType[ index ] = evt.newValue;
                _dataProp.intValue = (Int32)gdType.Data;
                _serializedObject.ApplyModifiedProperties();

                var nextCategory = category?.FindItem( evt.newValue )?.Subcategory;
                CreateWidgets( root, index + 1, nextCategory );

                CheckDuplicateType( root );
                CheckIncorrectType( gdType, (VisualElement)evt.target, index );
            }
        }

        private void AssignType( VisualElement root )
        {
            _serializedObject.Update();
            _dataProp.intValue = 1;
            _serializedObject.ApplyModifiedProperties();
            RecreateProperty( root );
        }

        private void ClearType( VisualElement root )
        {
            _serializedObject.Update();
            _dataProp.intValue = 0;
            _serializedObject.ApplyModifiedProperties();
            RecreateProperty( root );
        }

        private void FixType( VisualElement root )
        {
            _serializedObject.Update();
            if ( _gdoFinder.FindFreeType( new GdType( _dataProp.intValue ), out var newType ) ) 
            {
                _dataProp.intValue = (Int32)newType.Data;
                _serializedObject.ApplyModifiedProperties();
                RecreateProperty( root );
            }
        }

        private void OpenContextMenu( VisualElement root )
        {
            var menu = new GenericMenu();
            menu.AddItem( new GUIContent( "Copy" ), false, () => EditorGUIUtility.systemCopyBuffer = _dataProp.intValue.ToString() );
            if( Int32.TryParse( EditorGUIUtility.systemCopyBuffer, out _ ) )
                menu.AddItem( new GUIContent( "Paste" ), false, () =>
                {
                    if ( Int32.TryParse( EditorGUIUtility.systemCopyBuffer, out var gdTypeRawValue ) )
                    {
                        _serializedObject.Update();
                        _dataProp.intValue = gdTypeRawValue;
                        _serializedObject.ApplyModifiedProperties();
                        RecreateProperty( root );
                    }  
                } );
            else
                menu.AddDisabledItem( new GUIContent( "Paste" ), false );
            menu.AddItem( new GUIContent("Edit as Categories"), true, null );
            menu.AddItem( new GUIContent("Edit as decimal"), false, null );
            menu.AddItem( new GUIContent("Edit as hex"), false, null );     
            menu.ShowAsContext();
        }

        private void CheckDuplicateType( VisualElement root )
        {
            if ( _gdoFinder.IsDuplicatedType( GetGDType() ) )
            {
                root.AddToClassList( "duplicateType" );
                var toolbar    = GetToolbarFromRoot( root );
                var fixBtn     = toolbar.Q<Button>( "FixType" );
                if ( fixBtn == null )
                {
                    fixBtn                       = new Button( () => FixType(root) );
                    fixBtn.name                  = "FixType";
                    fixBtn.text                  = "";
                    fixBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "build_24dp" ) );
                    fixBtn.tooltip               = "Fix type";
                    fixBtn.AddToClassList( "toolbar-square-button" );
                    fixBtn.tabIndex = 1;
                    toolbar.Add( fixBtn );
                    toolbar.Sort( ToolbarSort );
                }
            }
            else
            {
                root.RemoveFromClassList( "duplicateType" );
                var toolbar    = GetToolbarFromRoot( root );
                var fixBtn = toolbar.Q<Button>( "FixType" );
                if( fixBtn != null )
                    toolbar.Remove( fixBtn );
            }
        }

        private void CheckIncorrectType( GdType type, VisualElement widget, Int32 index )
        {
            if ( !_typeHierarchy.IsTypeCorrect( type, out var incorrectIndex ) && index == incorrectIndex )
            {
                widget.AddToClassList( "incorrectValue" );
            }
            else
            {
                widget.RemoveFromClassList( "incorrectValue" );
            }
        }

        private Int32 ToolbarSort( VisualElement a, VisualElement b )
        {
            return a.tabIndex.CompareTo( b.tabIndex );
        }

        private VisualElement GetGDTypeFromRoot( VisualElement root )
        {
            return root.Q<VisualElement>( "GDType" );
        }

        private VisualElement GetToolbarFromRoot( VisualElement root )
        {
            return root.Q<VisualElement>( "Toolbar" );
        }

        private GdType GetGDType( )
        {
            return new GdType( _dataProp.intValue );
        }

        public class State
        {
            public String                         Label;
            public GdType                         Type;
            public String                         Value;
            public List<GDTypeHierarchy.Category> Categories = new();
            public List<Button>                   Buttons;

            public class Button
            {
                public EButton Type;
                public Action Click;
            }

            public enum EButton
            {
                AssignType,
                ClearType,
                FixType,
                ContextMenu,
            }


        }

        public static class Resources
        {
            public static Sprite AssignTypeIcon => UnityEngine.Resources.Load<Sprite>( "add_24dp" );
            public static Sprite FixTypeIcon => UnityEngine.Resources.Load<Sprite>( "build_24dp" );
            public static Sprite ClearTypeIcon => UnityEngine.Resources.Load<Sprite>( "delete_forever_24dp" );
        }


    }

    
    
}