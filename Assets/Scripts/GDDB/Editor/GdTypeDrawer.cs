using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(GdType) )]
    public class GdTypeDrawer : PropertyDrawer
    {
        private VisualElement       _typeWidgetsContainer;

        private          SerializedObject         _serializedObject;
        private SerializedProperty                _dataProp;
        private          VisualElement            _buttonsContainer;
        private readonly GDTypeHierarchy          _typeHierarchy;
        private readonly GDTypeHierarchy.Category _root;

        public GdTypeDrawer( )
        {
            _typeHierarchy = new GDTypeHierarchy();
            _root = _typeHierarchy.Root;
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            _serializedObject = property.serializedObject;
            _dataProp         = property.FindPropertyRelative( nameof(GdType.Data) );
      
            var root = new VisualElement();

            root.style.flexDirection = FlexDirection.Row;
            var label = new Label( property.displayName );
            label.style.flexBasis = new StyleLength( new Length( 41, LengthUnit.Percent ) );
            label.style.minWidth = 130;
            root.Add( label );

            _typeWidgetsContainer                     = new VisualElement();
            _typeWidgetsContainer.style.flexDirection = FlexDirection.Row;
            _typeWidgetsContainer.style.flexGrow      = 1;
            //_typeWidgetsContainer.AddToClassList( "unity-base-field__aligned" );        
            root.Add( _typeWidgetsContainer );
            //var label = new Label( property.displayName );
            //label.AddToClassList( "unity-property-field__label" );
            //label.AddToClassList( "unity-base-field__label" );
            //label.AddToClassList( "unity-object-field__label" );
            //container.Add( label );

            _buttonsContainer = new VisualElement();
            _buttonsContainer.style.flexDirection = FlexDirection.Row;
            root.Add( _buttonsContainer );

            RecreateProperty();

            return root;
        }

        private void RecreateProperty( )
        {
            _typeWidgetsContainer.Clear();
            _buttonsContainer.Clear();

            if( _dataProp.intValue == 0 )
            {
                CreateNoneType( _typeWidgetsContainer );
            }
            else
            {
                CreateWidgets( 0, _root );
                var clearTypeBtn = new Button( ClearType );
                clearTypeBtn.text = "X";
                _buttonsContainer.Add( clearTypeBtn );
            }
        }

        private void CreateNoneType( VisualElement root )
        {
            var noneLabel = new Label("None");
            _typeWidgetsContainer.Add( noneLabel );

            var addTypeBtn = new Button( AssignType );
            addTypeBtn.text = "+";
            _buttonsContainer.Add( addTypeBtn );
        }

        private void CreateWidgets( Int32 index, GDTypeHierarchy.Category category )
        {
            if( index >= 4 )
                return;

            Debug.Log( $"Creating widget at {index} position" );

            //Clear this widget and all next
            while ( _typeWidgetsContainer.childCount > index )
                _typeWidgetsContainer.RemoveAt( index );

            var gdType = new GdType( _dataProp.intValue );
            var widget = CreateCategoryField( category, gdType[ index ], index );
            _typeWidgetsContainer.Add( widget );

            //Create next widget(s)
            var nextCategory = category?.FindItem( gdType[ index ] )?.Subcategory;
            CreateWidgets( index + 1, nextCategory );
        }

        private VisualElement CreateCategoryField( GDTypeHierarchy.Category category, Int32 value, Int32 index )
        {
            VisualElement result = null;
            if ( category == null )         //Default Int8 field
            {
                result                = new IntegerField( 3 ){ value = value };
                result.style.minWidth = 50;
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>, Int32>( OnChangeEnumWidget, index  );

                Debug.Log( $"Created int field, value {value}" );
            }
            else if ( category.Type == GDTypeHierarchy.CategoryType.Enum )
            {
                result = new PopupField<Int32>( category.Items.Select( i => i.Value ).ToList(), value, i => category.FindItem( i ).Name,
                        i => category.FindItem( i ).Name );
                result.style.flexGrow = 1;
                result.RegisterCallback<ChangeEvent<Int32>, Int32>( OnChangeEnumWidget, index  );

                Debug.Log( $"Created popup field for {category.UnderlyingType} category, value {category.FindItem( value ).Name}" );
            }

            return result;

            //Rebuild widgets after change
            void OnChangeEnumWidget( ChangeEvent<Int32> evt, Int32 index )
            {
                _serializedObject.Update();
                var gdType = new GdType( _dataProp.intValue );
                gdType[ index ] = evt.newValue;
                _dataProp.intValue = (Int32)gdType.Data;
                _serializedObject.ApplyModifiedProperties();

                var nextCategory = category?.FindItem( evt.newValue )?.Subcategory;
                CreateWidgets( index + 1, nextCategory );
            }
        }

        private void AssignType( )
        {
            _serializedObject.Update();
            _dataProp.intValue = 1;
            _serializedObject.ApplyModifiedProperties();
            RecreateProperty();
        }

        private void ClearType( )
        {
            _serializedObject.Update();
            _dataProp.intValue = 0;
            _serializedObject.ApplyModifiedProperties();
            RecreateProperty();
        }
    }

    
    
}