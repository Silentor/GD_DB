using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace GDDB.Editor
{
    public class SearchPopup : PopupWindowContent
    {
        public override void OnGUI(Rect rect )
        {
            //see OnOpen
        }

        public override Vector2 GetWindowSize( )
        {
            var baseSize = base.GetWindowSize();
            baseSize.y = 400;
            return baseSize;
        }

        public override void OnOpen( )
        {
            base.OnOpen();

            var asset    = Resources.Load<VisualTreeAsset>( "SearchPopup" );
            var instance = asset.Instantiate();
            editorWindow.rootVisualElement.Add( instance );

            _itemAsset = Resources.Load<VisualTreeAsset>( "SearchPopupItem" );

            var searchField = instance.Q<TextField>( "SearchField" );
            searchField.RegisterValueChangedCallback( SearchFieldOnChanged );

            _resultsList = instance.Q<ListView>( "ResultsList" );
            _resultsList.makeItem    =  () => _itemAsset.Instantiate();
            _resultsList.bindItem    =  ResultsListOnBindItem;
            _resultsList.itemsSource = _results;

            var allComponentTypes   = TypeCache.GetTypesDerivedFrom( typeof(GDComponent) );
            _allProperComponents = allComponentTypes.Where( t => !t.IsAbstract ).OrderBy( t => t.Name ).ToArray();
            _allProperNames      = _allProperComponents.Select( t => t.Name ).ToList();
            _items = _allProperComponents.Select( t => new Item { Namespace = t.Namespace.Split( '.' ).ToList(), ComponentName = t.Name } ).ToList();

            PrepareResult( String.Empty );
        }

        private void ResultsListOnBindItem(VisualElement e, Int32 i )
        {
            var resultItem = _results[ i ];
            var itemBtn    = e.Q<Button>( "ItemBtn" );
            var label      = itemBtn.Q<Label>( "Label" );
            label.text = resultItem.Label;
            var isNamespace = resultItem.IsNamespace;
            var nsWidget    = e.Q<VisualElement>( "Next" );
            nsWidget.style.display = isNamespace ? DisplayStyle.Flex : DisplayStyle.None;
        }


        private readonly List<ResultItem> _results = new List<ResultItem>();
        private          Type[]           _allProperComponents;
        private          List<String>     _allProperNames;
        private          ListView         _resultsList;
        private          List<Item>       _items;
        private          VisualTreeAsset  _itemAsset;

        void SearchFieldOnChanged(ChangeEvent<String> ev )
        {
            // _results.Clear();
            // if( String.IsNullOrWhiteSpace( ev.newValue ) )
            //     _results.AddRange( _allProperNames );
            // else
            //     _results.AddRange( _allProperNames.Where( n => n.Contains( ev.newValue, StringComparison.OrdinalIgnoreCase ) ) );
            // _resultsList.RefreshItems();
        }

         private void PrepareResult( String searchString )
         {
             _results.Clear();
             if ( String.IsNullOrWhiteSpace( searchString ) )
             {
                 var namespaces = _items.Select( i => i.Namespace.First() ).Distinct().ToList();
                 foreach ( var ns in namespaces )
                 {
                     _results.Add( new ResultItem { Label = ns, IsNamespace = true } );
                 }
             }

             _resultsList.RefreshItems();
        
         }

        public struct Item
        {
            public List<String> Namespace;
            public String       ComponentName;
        }

        public struct ResultItem
        {
            public String Label;
            public Boolean IsNamespace;
        }

    }
}