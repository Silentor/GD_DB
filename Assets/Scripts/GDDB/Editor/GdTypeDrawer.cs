using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
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
        private readonly StyleSheet               _gdTypeStyles;
        
        private readonly GDObjectsFinder          _gdoFinder;

        private State  _state;
        private UInt32? _lastGdTypeValue;

        public GdTypeDrawer( )
        {
            _typeHierarchy = new GDTypeHierarchy();
            _gdTypeStyles = UnityEngine.Resources.Load<StyleSheet>( "GDType" );
            _gdoFinder = new GDObjectsFinder();

            //Debug.Log( $"Constructor" );
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label )
        {
            _serializedObject = property.serializedObject;
            _serializedObject.Update();
            
            _dataProp = property.FindPropertyRelative( nameof(GdType.Data) );

            if( _lastGdTypeValue != _dataProp.uintValue )
            {
                _state = null;
                _lastGdTypeValue = _dataProp.uintValue;
            }

            if ( _state == null )
                _state = CreateState( label.text, _dataProp );

            DrawStateIMGUI( position, label, _dataProp, _state );
        }

#region State

        private State CreateState( String label, SerializedProperty gdTypeProp )
        {
            var gdType = new GdType( gdTypeProp.uintValue );
            if ( gdType == default )
                return CreateNoneTypeState( label, gdTypeProp );
            else
            {
                if ( _serializedObject.targetObject is GDObject )
                    return CreateAssetTypeState( label, gdTypeProp, gdType );
                else
                    return CreateReferenceTypeState( label, gdTypeProp, gdType );
            }
        }   

        private State CreateAssetTypeState( String label, SerializedProperty gdTypeProp, GdType gdType )
        {
            var result = new State()
                   {
                           Label = label,
                           Type  = gdType,
                           Buttons =
                           {
                                   new State.Button()
                                   {
                                           Type = State.EButton.ContextMenu,
                                           Click = OpenContextMenu,
                                   },
                                   new State.Button()
                                   {
                                           Type = State.EButton.ClearType,
                                           Click = ClearType
                                   }
                           }
                   };

            var metadata = _typeHierarchy.GetMetadataOf( gdType );
            for ( int i = 0; i < metadata.Categories.Count; i++ )
            {
                result.Categories.Add( GetStateCategoryValue( metadata.Categories[i], gdType[i] ) );    
            }

            if ( !metadata.IsTypeDefined( gdType ) )
            {
                result.IsError      = true;
                result.ErrorMessage = $"Type not properly defined. Press Fix button to clear undefined part";
                result.Buttons.Add( new State.Button()
                                    {
                                            Type  = State.EButton.FixType,
                                            Click = FixUndefinedTypePart,
                                    } );
            }
            else if ( !metadata.IsTypeInRange( gdType, out var incorrectCategory ) )
            {
                result.IsError      = true;
                result.ErrorMessage = $"Category {incorrectCategory.Index+1} value is out of range. Select proper value of press Fix button to autofix";
                result.Buttons.Add( new State.Button()
                                    {
                                            Type  = State.EButton.FixType,
                                            Click = FixCategoryOutOfRange,
                                    } );
            }
            else if( _gdoFinder.IsDuplicatedType( gdType, metadata, out var count ) )
            {
                result.IsError = true;
                result.ErrorMessage = $"Duplicate type, count {count}. Press Fix button to assign new type";
                result.Buttons.Add( new State.Button()
                                   {
                                           Type = State.EButton.FixType,
                                           Click = FixDuplicateType,
                                   } );
            }

            return result;
        }

        private State CreateReferenceTypeState( String label, SerializedProperty gdTypeProp, GdType gdType )
        {
            var result = new State()
                         {
                                 Label = label,
                                 Type  = gdType,
                                 Buttons =
                                 {
                                         new State.Button()
                                         {
                                                 Type  = State.EButton.ContextMenu,
                                                 Click = OpenContextMenu,
                                         },
                                         new State.Button()
                                         {
                                                 Type  = State.EButton.ClearType,
                                                 Click = ClearType
                                         }
                                 }
                         };
        
            var metadata = _typeHierarchy.GetMetadataOf( gdType );
            var existCategoryValues = GetExistCategoryItems( metadata.Categories, gdType );
            var filteredCategoryValues = GetFilteredItems( existCategoryValues, gdType );

            var categoryValues = new List<State.CategoryValue>( filteredCategoryValues.Count );
            for ( var i = 0; i < filteredCategoryValues.Count; i++ )
            {
                var category = metadata.Categories[ i ];
                var value    = category.GetValue( gdType );
                var isError  = filteredCategoryValues[ i ].All( ci => ci.Value != value );
                var errorMsg = String.Empty;
                if ( isError )
                {
                    if ( category.Type == GDTypeHierarchy.CategoryType.Enum )
                        errorMsg = $"Invalid value {value} of {category.UnderlyingType.Name}";
                    else
                        errorMsg = $"Invalid value {value}";
                    var errorItem = new GDTypeHierarchy.CategoryItem( errorMsg, value, category );
                    filteredCategoryValues[ i ].Add( errorItem );
                }
                categoryValues.Add( new State.CategoryValue(  )
                                      {
                                              ActualItems = filteredCategoryValues[ i ],
                                              Category = metadata.Categories[ i ],
                                              Value = value,
                                              IsError = isError,
                                              ErrorMessage = errorMsg,
                                      } );
            }
            result.Categories.AddRange( categoryValues );

            if ( result.Categories.TryFirst( c => c.IsError, out var errorCategory ) )
            {
                result.IsError = true;
                result.ErrorMessage = errorCategory.ErrorMessage;
            }

            return result;
        }

        private State CreateNoneTypeState( String label, SerializedProperty gdTypeProp )
        {
            return new State()
                   {
                           Label = label,
                           Value = "None",
                           Buttons = 
                           {
                                   new State.Button()
                                   {
                                           Type = State.EButton.AssignType,
                                           Click = AssignType
                                   },
                                   new State.Button()
                                   {
                                        Type = State.EButton.ContextMenu,
                                        Click = OpenContextMenu,
                                    },

                           }
                   };

            
        }

        private State.CategoryValue GetStateCategoryValue( GDTypeHierarchy.Category category, Int32 value )
        {
            var isOutOfCategoryValue = !category.IsCorrectValue( value );
            var result =  new State.CategoryValue()
                   {
                           Category     = category,
                           Value        = value,
                           IsError      = isOutOfCategoryValue,
                           ErrorMessage = isOutOfCategoryValue ? $"Incorrect value {value}" : String.Empty,
                   };
            if ( isOutOfCategoryValue )
                result.ActualItems = new List<GDTypeHierarchy.CategoryItem>( category.Items.Append( new GDTypeHierarchy.CategoryItem( $"Incorrect value {value}", value, category ) ) );

            return result;
        }

        private List<List<GDTypeHierarchy.CategoryItem>>  GetExistCategoryItems( IReadOnlyList<GDTypeHierarchy.Category> categories, GdType type )
        {
            var result     = new List<List<GDTypeHierarchy.CategoryItem>>();
            var existTypes = _gdoFinder.GDTypedObjects.Select( g => g.Type ).ToList();
            for ( var i = 0; i < categories.Count; i++ )
            {
                if( i > 0 )
                {
                    existTypes.RemoveAll( t => t[ i - 1 ] != type[ i - 1 ] );
                }

                var category       = categories[ i ];
                var distinctValues = existTypes.Select( t => category.GetValue( t ) ).Distinct().ToList();
                var filteredItems = category.Items.Where( ci => distinctValues.Contains( ci.Value ) ).ToList();

                // var isError      = filteredItems.All( ci => ci.Value != type[ i ] );
                // if ( isError )
                // {
                //     filteredItems.Add( new GDTypeHierarchy.CategoryItem( $"Incorrect value {type[ i ]}", type[ i ] ) );
                // }
                //var errorMessage = isError ? $"Incorrect value {type[ i ]}" : String.Empty;
                result.Add( filteredItems);
            }

            return result;
        }

        private List<List<GDTypeHierarchy.CategoryItem>> GetFilteredItems( List<List<GDTypeHierarchy.CategoryItem>> categories, GdType typeValue )
        {
            var filterAttributes = fieldInfo.GetCustomAttributes<GdTypeFilterAttribute>().ToList();

            if( filterAttributes.Count == 0 )
                return categories;

            var result = new List<List<GDTypeHierarchy.CategoryItem>>( categories.Count );
            for ( var i = 0; i < categories.Count; i++ )
            {
                var categoryItems = new List<GDTypeHierarchy.CategoryItem>();
                for ( var j = 0; j < filterAttributes.Count; j++ )
                {
                    var filterAttribute = filterAttributes[ j ];

                    if ( i > 0 && i - 1 < filterAttribute.FilterCategories.Count )            //Check is filter applicable
                    {
                        if ( typeValue[ i - 1 ] != filterAttribute.FilterCategories[ i - 1 ] )         //todo Get filter categories and compare categories
                        {
                            filterAttributes.RemoveAt( j );
                            j--;
                            continue;
                        }
                    }
                    
                    if ( filterAttribute.FilterCategories.TryElementAt( i, out var filterValue ) )
                        categoryItems.AddRange( categories[ i ].Where( c => c.Value == filterValue ) );
                    else
                        categoryItems.AddRange( categories[ i ] );
                }

                result.Add( categoryItems );
            }

            return result;
        }

        private void AssignType( )
        {
            _serializedObject.Update();
            _dataProp.uintValue = 1;
            _serializedObject.ApplyModifiedProperties();
        }

        private void ClearType(  )
        {
            _serializedObject.Update();
            _dataProp.uintValue = 0;
            _serializedObject.ApplyModifiedProperties();
        }

        private void FixDuplicateType(  )
        {
            _serializedObject.Update();
            if ( _gdoFinder.FindFreeType( new GdType( _dataProp.uintValue ), _typeHierarchy, out var newType ) ) 
            {
                _dataProp.uintValue = newType.Data;
                _serializedObject.ApplyModifiedProperties();
            }
        }

        private void FixCategoryOutOfRange(  )
        {
            _serializedObject.Update();
            var gdType = new GdType( _dataProp.uintValue );
            if ( !_typeHierarchy.IsTypeInRange( gdType, out var incorrectCategory ) )
            {
                var incorrectValue = incorrectCategory.GetValue( gdType );
                if ( incorrectCategory.Type == GDTypeHierarchy.CategoryType.Enum )
                {
                    var diff         = Int32.MaxValue;
                    var optimalValue = 0;
                    foreach ( var categoryItem in incorrectCategory.Items )
                    {
                        var value = categoryItem.Value;
                        if( Math.Abs( value - incorrectValue ) < diff )
                        {
                            diff = Math.Abs( value - incorrectValue );
                            optimalValue = value;
                        }
                    }
                    if( diff < Int32.MaxValue )
                    {
                        incorrectCategory.SetValue( ref gdType, optimalValue );
                        _dataProp.uintValue = gdType.Data;
                        _serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    incorrectValue = Mathf.Clamp( incorrectValue, incorrectCategory.MinValue, incorrectCategory.MaxValue );
                    incorrectCategory.SetValue( ref gdType, incorrectValue );
                    _dataProp.uintValue = gdType.Data;
                    _serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void FixUndefinedTypePart(  )
        {
            _serializedObject.Update();
            var oldType  = new GdType( _dataProp.uintValue );
            var metadata = _typeHierarchy.GetMetadataOf( oldType );
            var newType  = metadata.ClearUndefinedTypePart( oldType );
            if( newType != oldType )
            {
                _dataProp.uintValue = newType.Data;
                _serializedObject.ApplyModifiedProperties();
            }
        }

        private void OpenContextMenu( )
        {
            var gdType = new GdType( _dataProp.uintValue );

            var menu = new GenericMenu();
            menu.AddItem( new GUIContent( "Copy" ), false, () => EditorGUIUtility.systemCopyBuffer = _dataProp.uintValue.ToString() );
            if( UInt32.TryParse( EditorGUIUtility.systemCopyBuffer, out var gdTypeRawValue ) )
            {
                var typeString = _typeHierarchy.GetTypeString( new GdType( gdTypeRawValue ) );
                menu.AddItem( new GUIContent( $"Paste {typeString}" ), false, () =>
                {
                    _serializedObject.Update();
                    _dataProp.uintValue = gdTypeRawValue;
                    _serializedObject.ApplyModifiedProperties();
                } );

            }
            else
                menu.AddDisabledItem( new GUIContent( "Paste" ), false );

            if ( gdType != default )
            {
                menu.AddItem( new GUIContent("Ping GD asset"),      false, () =>
                {
                    if( _gdoFinder.GDTypedObjects.TryFirst( g => g.Type == gdType, out var gdObject ) )
                        EditorGUIUtility.PingObject( gdObject );
                } );
            }

            menu.AddItem( new GUIContent("Edit as Categories"), EditMode == EEditMode.Categories, () => SetEditMode(EEditMode.Categories) );
            menu.AddItem( new GUIContent("Edit as raw"),        EditMode == EEditMode.Raw,        () => SetEditMode(EEditMode.Raw) );

            menu.ShowAsContext();
        }

        private void SetEditMode( EEditMode mode )
        {
            EditMode = mode;
            //Repaint property for UITookit mode but not for IMGUI
        }

        public EEditMode EditMode
        {
            get => (EEditMode)EditorPrefs.GetInt( "GDDB.Editor.GdTypeDrawer", 0 );
            set => EditorPrefs.SetInt( "GDDB.Editor.GdTypeDrawer", (Int32)value );
        }

#endregion

#region IMGUI
        
       private void DrawStateIMGUI( Rect position, GUIContent label, SerializedProperty prop, State state )
       {
           label = EditorGUI.BeginProperty( position, label, prop );

           //DEBUG
           var gdType = new GdType( prop.uintValue );
           if( gdType != default )
               label.text = $"{label.text} ({gdType.ToString()})";
           //DEBUG

            //Draw label
            position = EditorGUI.PrefixLabel( position, label, state.IsError ? Resources.PrefixLabelErrorStyle : Resources.PrefixLabelStyle );
            position.height = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

            if ( state.IsError )
            {
                EditorGUI.HelpBox( position, state.ErrorMessage, MessageType.Error );
                position.y += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 2;
            }

            var toolbarWidth = 20 * state.Buttons.Count;
            var toolbarPosition = new Rect( position.x + position.width - toolbarWidth, position.y, toolbarWidth, position.height );
            var typeWidth = position.width - toolbarWidth;
            var typePosition = new Rect( position.x, position.y, typeWidth, position.height );

            
            switch ( EditMode )
            {
                //Draw Categories or special value
                case EEditMode.Categories:
                {
                    if( state.Value != null )
                        GUI.Label( typePosition, state.Value );

                    if ( state.Categories.Count > 0 )
                    {
                        var categoryWidth    = typeWidth / state.Categories.Count;
                        var categoryPosition = new Rect( position.x, position.y, categoryWidth, position.height );
                        for ( var i = 0; i < state.Categories.Count(); i++ )
                        {
                            DrawCategoryIMGUI( categoryPosition, prop, state.Categories[ i ], i );
                            categoryPosition.x += categoryWidth;
                        }
                    }

                    break;
                }

                //Draw developer mode hex/dec value fields
                case EEditMode.Raw:
                {
                    //Hex mode
                    var rawFieldPosition = new Rect( typePosition.x, typePosition.y, 80, typePosition.height );
                    var oldValueStr = prop.uintValue.ToString( "X", Resources.NoGroupSeparatorFormat );
                    EditorGUI.BeginChangeCheck();
                    var newValueStr = GUI.TextField( rawFieldPosition, oldValueStr, 8, state.IsError ? Resources.TextFieldErrorStyle : GUI.skin.textField );
                    if ( EditorGUI.EndChangeCheck() && UInt32.TryParse( newValueStr, NumberStyles.HexNumber, Resources.NoGroupSeparatorFormat, out var newValue ) )
                    {
                        prop.uintValue = newValue;
                        prop.serializedObject.ApplyModifiedProperties();
                    }
                    rawFieldPosition.x += rawFieldPosition.width + 2;
                    GUI.Label( rawFieldPosition, "hex" );

                    //Dec mode
                    rawFieldPosition.x += 30;
                    rawFieldPosition.width = 100;
                    oldValueStr        =  prop.uintValue.ToString( "N0", Resources.NoGroupSeparatorFormat );
                    EditorGUI.BeginChangeCheck();
                    newValueStr = GUI.TextField( rawFieldPosition, oldValueStr, 13, state.IsError ? Resources.TextFieldErrorStyle : GUI.skin.textField );
                    if ( EditorGUI.EndChangeCheck() )
                    {
                        if ( String.IsNullOrWhiteSpace( newValueStr ) )
                            newValueStr = "0";
                        if ( UInt32.TryParse( newValueStr, NumberStyles.Integer, Resources.NoGroupSeparatorFormat, out newValue ) )
                        {
                            prop.uintValue = newValue;
                            prop.serializedObject.ApplyModifiedProperties();
                        }
                    }
                    rawFieldPosition.x += rawFieldPosition.width + 2;
                    GUI.Label( rawFieldPosition, "dec" );

                    //Dec mode by category
                    rawFieldPosition.x += 30;
                    rawFieldPosition.width = Math.Max( toolbarPosition.x - rawFieldPosition.x - 30, 0 );
                    EditorGUI.BeginChangeCheck();
                    var values = new Int32[]{state.Type[0], state.Type[1], state.Type[2], state.Type[3] };
                    EditorGUI.MultiIntField( rawFieldPosition, Resources.ComponentLabels, values );
                    if ( EditorGUI.EndChangeCheck() )
                    {
                        var newType = new GdType
                                      {
                                              [ 0 ] = values[0],
                                              [ 1 ] = values[1],
                                              [ 2 ] = values[2],
                                              [ 3 ] = values[3]
                                      };
                        prop.uintValue = newType.Data;
                        prop.serializedObject.ApplyModifiedProperties();
                    }
                    rawFieldPosition.x += rawFieldPosition.width + 2;
                    GUI.Label( rawFieldPosition, "dec" );

                    break;
                }

            }

            if( state.Buttons.Count > 0 )
            {
                //Draw toolbar
                GUI.BeginGroup( toolbarPosition );
                for ( var i = 0; i < state.Buttons.Count; i++ )
                {
                    var button = state.Buttons[ i ];
                    if ( GUI.Button( new Rect( i * 20, 0, 20, 20 ), new GUIContent(String.Empty, GetButtonIcon( button.Type ), GetButtonTooltip( button.Type )), Resources.ToolbarButtonStyle ) )
                    {
                        button.Click( );
                    }
                }
                GUI.EndGroup();
            }
            
            EditorGUI.EndProperty();
        }

        private void DrawCategoryIMGUI( Rect categoryPosition, SerializedProperty prop, State.CategoryValue category, Int32 index )
        {                 
            var gdType = new GdType( prop.uintValue );
            var value = category.Category.GetValue( gdType );
            var items = category.ActualItems ?? category.Category.Items;


            if ( items == null )                 //Items null - use category default values
            {
                if ( category.Category.Type != GDTypeHierarchy.CategoryType.Enum )
                {
                    DrawIntField( );
                }
                else
                {
                    DrawEmptyPopupField();
                }
            }
            else if ( items.Count == 0 )              //Show disabled controls
            {
                if ( category.Category.Type != GDTypeHierarchy.CategoryType.Enum )
                {
                    DrawDisabledIntField();
                }
                else
                {
                    DrawEmptyPopupField();
                }
            }
            else
            {
                if ( category.Category.Type != GDTypeHierarchy.CategoryType.Enum )
                {
                    DrawPopupField();   //Int but with options list (gd reference). same popup as enum ?
                }
                else
                {
                    DrawPopupField();
                }
            }
            
            void DrawIntField( )
            {
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.DelayedIntField( categoryPosition, value );
                if ( EditorGUI.EndChangeCheck() )
                {
                    GdType newGdType = default;
                    category.Category.SetValue( ref newGdType, newValue );
                    if ( newGdType != default && newGdType != gdType)
                    {
                        prop.uintValue   = newGdType.Data;
                        prop.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            void DrawPopupField( )
            {
                var namesList = items.Select( i => i.Name ).ToList();
                var oldIndex  = items.FindIndex( i => i.Value == value );
                var names     = namesList.ToArray();

                EditorGUI.BeginChangeCheck();
                var newIndex = EditorGUI.Popup( categoryPosition, oldIndex, names, category.IsError ? Resources.PopupErrorStyle : EditorStyles.popup );
                if ( EditorGUI.EndChangeCheck() )
                {
                    value           = items[ newIndex ].Value;
                    category.Category.SetValue( ref gdType, value );
                    if ( gdType != default )
                    {
                        prop.uintValue = gdType.Data;
                        prop.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            void DrawEmptyPopupField( )
            {
                GUI.enabled = false;
                EditorGUI.Popup( categoryPosition, 0, new [] { "No options" } );
                GUI.enabled = true;
            }

            void DrawDisabledIntField( )
            {
                GUI.enabled = false;
                EditorGUI.TextField( categoryPosition, "No options" );
                GUI.enabled = true;
            }
        } 

        private Texture2D GetButtonIcon( State.EButton type )
        {
            switch ( type )
            {
                case State.EButton.AssignType:
                    return Resources.AssignTypeIcon;
                case State.EButton.ClearType:
                    return Resources.ClearTypeIcon;
                case State.EButton.FixType:
                    return Resources.FixTypeIcon;
                case State.EButton.ContextMenu:
                    return Resources.ContextMenuIcon;
                default:
                    throw new ArgumentOutOfRangeException( nameof(type), type, null );
            }
        }

        private String GetButtonTooltip( State.EButton type )
        {
            switch ( type )
            {
                case State.EButton.AssignType:
                    return Resources.AssignTypeTooltip;
                case State.EButton.ClearType:
                    return Resources.ClearTypeTooltip;
                case State.EButton.FixType:
                    return Resources.FixTypeTooltip;
                case State.EButton.ContextMenu:
                    return Resources.ContextMenuTooltip;
                default:
                    throw new ArgumentOutOfRangeException( nameof(type), type, null );
            }
        }

        public override Single GetPropertyHeight(SerializedProperty property, GUIContent label )
        {
            if( _state != null && _state.IsError )
                return base.GetPropertyHeight( property, label ) * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
            else
                return base.GetPropertyHeight( property, label );
        }

    #endregion

        

        /*public override VisualElement CreatePropertyGUI( SerializedProperty property )
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
        }*/

        /*private void RecreateProperty( VisualElement root )
        {
            root.RemoveFromClassList( "duplicateType" );
            var gdType = GetGDTypeFromRoot( root );
            var toolbar = GetToolbarFromRoot( root );

            gdType.Clear();
            toolbar.Clear();

            if( _dataProp.uintValue == 0 )
            {
                CreateNoneType( root );
            }
            else
            {
                CreateWidgets( root, 0, _typeHierarchy.Root );

                var menuBtn = new Button( );
                menuBtn.clicked += OpenContextMenu;
                menuBtn.text                  = "";
                menuBtn.tabIndex              = 2;
                menuBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "menu_24dp" ) );
                menuBtn.tooltip               = "Open context menu";
                menuBtn.AddToClassList( "toolbar-square-button" );
                toolbar.Add( menuBtn );

                var clearTypeBtn = new Button( ClearType );
                clearTypeBtn.text                  = "";
                clearTypeBtn.tabIndex              = 10;
                clearTypeBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "delete_forever_24dp" ) );
                clearTypeBtn.tooltip               = "Clear type";
                clearTypeBtn.AddToClassList( "toolbar-square-button" );
                toolbar.Add( clearTypeBtn );

                CheckDuplicateType( root );
            }
        }*/

        /*private void  CreateNoneType( VisualElement root )
        {
            var noneLabel = new Label("None");
            var gdType    = GetGDTypeFromRoot( root );
            gdType.Add( noneLabel );

            var addTypeBtn = new Button( /*() => AssignType(root)#1# );
            addTypeBtn.text     = "";
            addTypeBtn.tabIndex = 1;
            addTypeBtn.AddToClassList( "toolbar-square-button" );
            addTypeBtn.style.backgroundImage = new StyleBackground( UnityEngine.Resources.Load<Sprite>( "add_24dp" ) );
            addTypeBtn.tooltip = "Assign type";
            var toolbar = GetToolbarFromRoot( root );
            toolbar.Add( addTypeBtn );
        }*/

        /*private void CreateWidgets( VisualElement root, Int32 index, GDTypeHierarchy.Category category )
        {
            if( index >= 4 )
                return;

            var gdType = GetGDTypeFromRoot( root );
            //Debug.Log( $"Creating widget at {index} position" );

            //Clear this widget and all next
            while ( gdType.childCount > index )
                gdType.RemoveAt( index );

            var gdTypeValue = new GdType( _dataProp.uintValue );
            var widget = CreateCategoryField( root, category, gdTypeValue[ index ], index );
            gdType.Add( widget );
            CheckIncorrectType( gdTypeValue, widget, index );

            //Create next widget(s)
            var nextCategory = category.GetItem( gdTypeValue[ index ] ).Subcategory;
            CreateWidgets( root, index + 1, nextCategory );
        }*/

        /*private VisualElement CreateCategoryField( VisualElement root, GDTypeHierarchy.Category category, Int32 value, Int32 index )
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

                Debug.Log( $"Created popup field, index {index} for {category.UnderlyingType} category, value {category.GetItem( value ).Name}" );

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
                        return category.GetItem( value ).Name;

                    return $"Incorrect value {value}";
                }
            }

            return result;

            //Rebuild widgets after change
            void OnChangeEnumWidget( ChangeEvent<Int32> evt )
            {
                _serializedObject.Update();
                var gdType = new GdType( _dataProp.uintValue );
                gdType[ index ] = evt.newValue;
                _dataProp.uintValue = gdType.Data;
                _serializedObject.ApplyModifiedProperties();

                var nextCategory = category.GetItem( evt.newValue ).Subcategory;
                CreateWidgets( root, index + 1, nextCategory );

                CheckDuplicateType( root );
                CheckIncorrectType( gdType, (VisualElement)evt.target, index );
            }
        }*/

        /*private void CheckDuplicateType( VisualElement root )
        {
            if ( _gdoFinder.IsDuplicatedType( GetGDType() ) )
            {
                root.AddToClassList( "duplicateType" );
                var toolbar    = GetToolbarFromRoot( root );
                var fixBtn     = toolbar.Q<Button>( "FixType" );
                if ( fixBtn == null )
                {
                    fixBtn                       = new Button( FixType );
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
        }*/

        /*private void CheckIncorrectType( GdType type, VisualElement widget, Int32 index )
        {
            if ( !_typeHierarchy.IsTypeCorrect( type, out var incorrectIndex ) && index == incorrectIndex )
            {
                widget.AddToClassList( "incorrectValue" );
            }
            else
            {
                widget.RemoveFromClassList( "incorrectValue" );
            }
        }*/

        /*private Int32 ToolbarSort( VisualElement a, VisualElement b )
        {
            return a.tabIndex.CompareTo( b.tabIndex );
        }*/

        /*private VisualElement GetGDTypeFromRoot( VisualElement root )
        {
            return root.Q<VisualElement>( "GDType" );
        }*/

        /*private VisualElement GetToolbarFromRoot( VisualElement root )
        {
            return root.Q<VisualElement>( "Toolbar" );
        }*/


        public class State
        {
            public          String                  Label;
            public          GdType                  Type;
            public          String                  Value;
            public readonly List<CategoryValue>     Categories = new();
            public readonly List<Button>            Buttons    = new ();

            public          Boolean                         IsError;
            public          String                          ErrorMessage;

            public class Button
            {
                public EButton          Type;
                public Action           Click;
            }

            public enum EButton
            {
                AssignType,
                FixType,
                ContextMenu,
                ClearType,
            }

            public class CategoryValue
            {
                public GDTypeHierarchy.Category           Category;
                public List<GDTypeHierarchy.CategoryItem> ActualItems;      //Has items - show popup, 0 items - show disabled field, null - use default items range from category
                public Int32                              Value;
                public Boolean                            IsError;
                public String                             ErrorMessage;
            }

        }

        public static class Resources
        {
            public static readonly GUIContent[] ComponentLabels = { GUIContent.none, GUIContent.none,  GUIContent.none, GUIContent.none };
            public static readonly Texture2D    AssignTypeIcon  = UnityEngine.Resources.Load<Texture2D>( "add_24dp" );
            public static readonly Texture2D    FixTypeIcon     = UnityEngine.Resources.Load<Texture2D>( "build_24dp" );
            public static readonly Texture2D    ClearTypeIcon   = UnityEngine.Resources.Load<Texture2D>( "delete_forever_24dp" );
            public static readonly Texture2D    ContextMenuIcon = UnityEngine.Resources.Load<Texture2D>( "menu_24dp" );
            public static readonly Texture2D    ErrorIcon       = UnityEngine.Resources.Load<Texture2D>( "error_24dp" );

            public static readonly Texture2D SolidRedTexture = UnityEngine.Resources.Load<Texture2D>( "solid_red" );

            public static readonly GUIStyle ToolbarButtonStyle = new ( GUI.skin.button )
                                                                 {
                                                                         //margin  = new RectOffset( 0, 0, 0, 0 ),
                                                                         padding = new RectOffset( 1, 1, 1, 1 ),
                                                                 };

            public static readonly GUIStyle PrefixLabelStyle = new ( EditorStyles.label ) { };
            public static readonly GUIStyle PrefixLabelErrorStyle = new ( PrefixLabelStyle )
                                                                    {
                                                                            //normal = new GUIStyleState(){background = SolidRedTexture },
                                                                            normal  = new GUIStyleState(){ textColor = Color.red },
                                                                            hover   = new GUIStyleState(){ textColor = Color.red },
                                                                            focused = new GUIStyleState(){ textColor = Color.red },
                                                                    };
            public static readonly GUIStyle PopupErrorStyle = new ( EditorStyles.popup )
                                                                    {
                                                                            normal  = new GUIStyleState(){ textColor = Color.red },
                                                                            hover   = new GUIStyleState(){ textColor = Color.red },
                                                                            focused = new GUIStyleState(){ textColor = Color.red },
                                                                    };

            public static readonly GUIStyle TextFieldErrorStyle = new (  GUI.skin.textField )
                                                                    {
                                                                            normal  = new GUIStyleState(){ textColor = Color.red },
                                                                            hover   = new GUIStyleState(){ textColor = Color.red },
                                                                            focused = new GUIStyleState(){ textColor = Color.red },
                                                                    };

            public static readonly Sprite AssignTypeSprite  = Sprite.Create( AssignTypeIcon, new Rect( 0, 0, AssignTypeIcon.width, AssignTypeIcon.height ), new Vector2( 0.5f, 0.5f ) );
            public static readonly Sprite FixTypeSprite     = Sprite.Create( FixTypeIcon, new Rect( 0, 0, FixTypeIcon.width, FixTypeIcon.height ), new Vector2( 0.5f, 0.5f ) );
            public static readonly Sprite ClearTypeSprite   = Sprite.Create( ClearTypeIcon, new Rect( 0, 0, ClearTypeIcon.width, ClearTypeIcon.height ), new Vector2( 0.5f, 0.5f ) ); 

            public static readonly String AssignTypeTooltip = "Assign type";
            public static readonly String FixTypeTooltip = "Fix duplicate or incorrect type";
            public static readonly String ClearTypeTooltip = "Set type to None";
            public static readonly String ContextMenuTooltip = "Open menu";

            public static readonly NumberFormatInfo NoGroupSeparatorFormat = new NumberFormatInfo( )
                                                                      {
                                                                              NumberGroupSeparator = "",
                                                                              NumberDecimalSeparator = ".",
                                                                      };

            public static readonly GUIStyle SmallLabelStyle = new GUIStyle( EditorStyles.label )
                                                              {
                                                                      fontSize = 9,
                                                              };
        }

        public enum EEditMode
        {
            Categories,
            Raw,
        }

    }

    
    
}