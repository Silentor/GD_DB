using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

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
        private          GDObject                 _owner;


        public GdTypeDrawer( )
        {
            _typeHierarchy = new GDTypeHierarchy();
            _typesRoot = _typeHierarchy.Root;
            _gdTypeStyles = Resources.Load<StyleSheet>( "GDType" );
            _gdoFinder = new GDObjectsFinder();
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            _serializedObject = property.serializedObject;
            _owner            = (GDObject)_serializedObject.targetObject;
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

                if ( _gdoFinder.IsDuplicatedType( _owner ) )
                {
                    var fixTypeBtn = new Button( () => FixType(root) );
                    fixTypeBtn.text                  = "";
                    fixTypeBtn.style.backgroundImage = new StyleBackground( Resources.Load<Sprite>( "build_24dp" ) );
                    fixTypeBtn.tooltip               = "Fix type";
                    fixTypeBtn.AddToClassList( "toolbar-square-button" );
                    toolbar.Add( fixTypeBtn );
                }

                var clearTypeBtn = new Button( () => ClearType(root) );
                clearTypeBtn.text                  = "";
                clearTypeBtn.style.backgroundImage = new StyleBackground( Resources.Load<Sprite>( "delete_forever_24dp" ) );
                clearTypeBtn.tooltip               = "Clear type";
                clearTypeBtn.AddToClassList( "toolbar-square-button" );
                toolbar.Add( clearTypeBtn );

                var myType = new GdType( _dataProp.intValue );
                if( _gdoFinder.GDTypedObjects.Any( o => o.Type == myType && o != _owner ) )
                    root.AddToClassList( "duplicateType" );
            }
        }


        private void CreateNoneType( VisualElement root )
        {
            var noneLabel = new Label("None");
            var gdType    = GetGDTypeFromRoot( root );
            gdType.Add( noneLabel );

            var addTypeBtn = new Button( () => AssignType(root) );
            addTypeBtn.text = "";
            addTypeBtn.AddToClassList( "toolbar-square-button" );
            addTypeBtn.style.backgroundImage = new StyleBackground( Resources.Load<Sprite>( "add_24dp" ) );
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
                result = new PopupField<Int32>( category.Items.Select( i => i.Value ).ToList(), value, i => category.FindItem( i ).Name,
                        i => category.FindItem( i ).Name );
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>>( OnChangeEnumWidget );

                Debug.Log( $"Created popup field, index {index} for {category.UnderlyingType} category, value {category.FindItem( value ).Name}" );
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

        private VisualElement GetGDTypeFromRoot( VisualElement root )
        {
            return root.Q<VisualElement>( "GDType" );
        }

        private VisualElement GetToolbarFromRoot( VisualElement root )
        {
            return root.Q<VisualElement>( "Toolbar" );
        }


    }

    
    
}