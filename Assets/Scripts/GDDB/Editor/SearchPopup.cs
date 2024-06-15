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
        

        public SearchPopup( GDObjectEditor editor, SerializedProperty componentsProp )
        {
            _editor              = editor;
            _componentsProp = componentsProp;
        }

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

            _resultsCountLabel = instance.Q<Label>( "ResultsCount" );

            _resultsList             = instance.Q<ListView>( "ResultsList" );
            _resultsList.makeItem    = ResultsList_MakeItem;
            _resultsList.bindItem    = ResultsList_BindItem;
            _resultsList.itemsSource = _results;

            //Prepare all components cache
            var allComponentTypes   = TypeCache.GetTypesDerivedFrom( typeof(GDComponent) );
            var allProperComponents = allComponentTypes.Where( t => !t.IsAbstract ).OrderBy( t => t.Name ).ToArray();
            _items = allProperComponents.Select( t => new Item
                                                       {
                                                               Namespace = t.Namespace.Split( '.' ).ToList(), 
                                                               ComponentName = t.Name, 
                                                               Type = t
                                                       } ).ToList();

            ProcessSearch( searchField.value );
        }

        

        private readonly GDObjectEditor      _editor;
        private readonly SerializedProperty  _componentsProp;
        private readonly List<ResultItem>    _results = new List<ResultItem>();
        private          ListView            _resultsList;
        private          IReadOnlyList<Item> _items;
        private          VisualTreeAsset     _itemAsset;
        private          Label               _resultsCountLabel;

        private VisualElement ResultsList_MakeItem( )
        {
            var result = _itemAsset.Instantiate();
            var btn    = result.Q<Button>( "ItemBtn" );
            btn.clicked += () => OnItemClicked( (ResultItem) btn.userData );
            return result;
        }

        private void ResultsList_BindItem( VisualElement e, Int32 i )
        {
            var resultItem = _results[ i ];
            var itemBtn    = e.Q<Button>( "ItemBtn" );
            var label      = itemBtn.Q<Label>( "Label" );
            label.text    = resultItem.Label;
            if ( !resultItem.IsNamespace )
            {
                label.tooltip = String.Concat( String.Join( ".", resultItem.Item.Namespace ), ".", resultItem.Item.ComponentName );
                itemBtn.Q<VisualElement>( "NamespaceIcon" ).style.display = DisplayStyle.None;
                itemBtn.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = DisplayStyle.None;
            }
            else
            {
                label.tooltip                                                 = String.Empty;
                itemBtn.Q<VisualElement>( "NamespaceIcon" ).style.display     = StyleKeyword.Null;
                itemBtn.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = StyleKeyword.Null;
            }

            itemBtn.userData = resultItem;
        }

        private void OnItemClicked( ResultItem resultItem )
        {
            if ( !resultItem.IsNamespace )
            {
                _editor.AddComponent( _componentsProp, resultItem.Item.Type );
                editorWindow.Close();
            }
            else            //Show selected namespace classes
            {

            }
        }

        

        void SearchFieldOnChanged(ChangeEvent<String> ev )
        {
             ProcessSearch( ev.newValue );
        }

        private void ProcessSearch( String searchString )
        {
            var results = SearchItems( searchString );
            PrepareResults( results );
            ShowResults( results );
        }
        

        private Result SearchItems( String searchString )
        {
            if ( String.IsNullOrWhiteSpace( searchString ) )
            {
                return new Result() { Items = _items.ToList() };
            }
            else
            {
                return new Result()
                       {
                               Items = _items.Where( i =>
                                       i.ComponentName.Contains( searchString, StringComparison.OrdinalIgnoreCase ) ).ToList()
                       };
            }
        }

         private void PrepareResults( Result result )
         {
             result.View = result.Items.Select( i => new ResultItem() { Item = i, Label = i.ComponentName } ).ToList();

             CompactResultView( result );          

             result.View.Sort((x, y) => String.Compare( x.Label, y.Label, StringComparison.Ordinal ));
         }

         private void CompactResultView( Result result )
         {
             if ( result.View.Count > 20 )
             {
                 var groupsByNamespace = result.View.Where( i => !i.IsNamespace && i.Item.Namespace.Any() ).GroupBy( i => i.Item.Namespace[ 0 ] )
                                              .OrderByDescending( g => g.Count() ).ToArray();

                 foreach ( var mostCommonNamespace in groupsByNamespace )
                 {
                     if ( mostCommonNamespace.Count() > 1 )
                     {
                         var groupedItem   = new ResultItem(){IsNamespace = true, SubitemsCount = mostCommonNamespace.Count(), 
                                                                     Label = $"{mostCommonNamespace.Key} ({mostCommonNamespace.Count()})"};
                         result.View.RemoveAll( i => !i.IsNamespace && i.Item.Namespace.Any() && i.Item.Namespace[0] == mostCommonNamespace.Key );
                         result.View.Add( groupedItem );

                         //Finished compacting
                         if( result.View.Count <= 20 )
                             break;
                     }
                     else
                     {
                         //Cannot compact
                         return;
                     }
                 }
             }

         }

         private void ShowResults( Result result )
         {
             if( result.Namespace == null )
                _resultsCountLabel.text = $"Results: {result.Items.Count}";
             else
             {
                 _resultsCountLabel.text = String.Join(".", result.Namespace );
             }
             _results.Clear();
             _results.AddRange( result.View );
             _resultsList.RefreshItems();
         }


        public struct Item
        {
            public List<String> Namespace;
            public String       ComponentName;
            public Type         Type;
        }

        public struct ResultItem
        {
            public String  Label;
            public Boolean IsNamespace;

            //Namespace mode
            public Int32   SubitemsCount;

            //Item mode
            public Item    Item;
        }

        public class Result
        {
            public List<Item>       Items;
            public List<ResultItem> View;
            public List<String>     Namespace;
        }

    }
}