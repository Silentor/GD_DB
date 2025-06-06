using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Gddb.Editor
{
    public class TypeSearchWidget
    {
        public TypeSearchWidget(   TypeSearchSettings settings, IReadOnlyList<Type> types )
        {
            _settings   = settings;
            _allTypes = types.Select(  t => new Item
                                          {
                                                  Namespace     = t.Namespace == null ? Array.Empty<String>() : t.Namespace.Split( '.' ).ToList(),
                                                  ComponentName = t.Name,
                                                  Type          = t
                                          } ).ToList();
        }

        public Type SelectedType { get; private  set; }


        public VisualElement CreateGUI( )
        {
            var instance = Resources.Window.Instantiate();
            _itemAsset = Resources.Item;

            _searchToolBtn = instance.Q<Button>( "SearchBtn" );
            _searchToolBtn.clicked += SearchToolBtn_Clicked;
            _recommendedToolBtn = instance.Q<Button>( "RecommendedBtn" );
            _recommendedToolBtn.clicked += RecommendedToolBtn_Clicked;

            _searchField = instance.Q<TextField>( "SearchField" );
            _searchField.RegisterValueChangedCallback( SearchField_Changed );
            _searchField.RegisterCallback<AttachToPanelEvent>( _ => _searchField.Focus() );

            _resultsLabel         =  instance.Q<Label>( "ResultsLbl" );
            _backNamespaceIcon    =  instance.Q<VisualElement>( "BackNamespaceIcon" );
            _resultsBtn           =  instance.Q<Button>( "ResultsBtn" );
            _resultsBtn.clicked += ResultsBtn_Clicked;
            _resultsBtn.RemoveFromClassList( "unity-button" );

            _resultsList             = instance.Q<ListView>( "ResultsList" );
            _resultsList.makeItem    = ResultsList_MakeItem;
            _resultsList.bindItem    = ResultsList_BindItem;
            _resultsList.unbindItem  = ResultsList_UnbindItem;
            _resultsList.itemsSource = _resultsToList;

            //Restore prev state
            var types = new List<Type>();
            _settings.LoadMRUComponents( types );
            _mru.AddRange( types.Where( t => _allTypes.Any( a => a.Type == t ) ) );             
            _settings.LoadFavoriteComponents( types );
            _favorites.AddRange( types.Where( t => _allTypes.Any( a => a.Type == t ) ) );

            _lastSearchResult  = new Result() { SearchString = _settings.LastSearchString, Items = _allTypes.ToList() };
            _searchField.SetValueWithoutNotify( _settings.LastSearchString );

            if ( _settings.SearchListMode == EListMode.Recommended )
                ProcessRecommended();
            else
                ProcessSearch( );

            //To support keyboard navigation
            instance.RegisterCallback<KeyDownEvent>( Widget_KeyDown, TrickleDown.TrickleDown );

            return instance;
        }

        /// <summary>
        /// When user select type
        /// </summary>
        public event Action<Type> Selected; 

        private readonly List<ResultItem>    _resultsToList = new ();
        private          ListView            _resultsList;
        private readonly IReadOnlyList<Item> _allTypes;
        private          VisualTreeAsset     _itemAsset;
        private          Label               _resultsLabel;
        private          Result              _lastSearchResult;
        private          VisualElement       _backNamespaceIcon;
        private          TextField           _searchField;
        private          Button              _resultsBtn;
        private          Button              _searchToolBtn;
        private          Button              _recommendedToolBtn;
        private readonly TypeSearchSettings  _settings;
        private          Int32               _selectedViewItem;

        private readonly List<Type> _mru       = new();
        private readonly List<Type> _favorites = new();
        

        private VisualElement ResultsList_MakeItem( )
        {
            var result = _itemAsset.Instantiate();
            var itemBtn    = result.Q<Button>( "ItemBtn" );
            itemBtn.clicked += ( ) => ItemBtn_Clicked( (ResultItem) itemBtn.userData );
            var iconBtn = result.Q<Button>( "IconBtn" );
            iconBtn.clicked += ( ) => IconBtn_Clicked( (ResultItem) iconBtn.userData );
            return result;
        }

        private void ResultsList_BindItem( VisualElement e, Int32 i )
        {
            var resultItem = _resultsToList[ i ];
            var itemBtn    = e.Q<Button>( "ItemBtn" );
            var iconBtn    = e.Q<Button>( "IconBtn" );
            iconBtn.style.backgroundImage = resultItem.Icon;
            var label      = itemBtn.Q<Label>( "Label" );
            label.text    = resultItem.RichLabel;
            
            if ( !resultItem.IsNamespace )
            {
                label.tooltip = String.Join( ".", resultItem.Item.Namespace.Append( resultItem.Item.ComponentName ) );
                e.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = DisplayStyle.None;
            }
            else
            {
                label.tooltip  = String.Join( ".", _lastSearchResult.InspectNamespace.Append( resultItem.Label ) );
                e.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = StyleKeyword.Null;
            }

            if( resultItem.IsSelected )
                e.AddToClassList( Resources.SelectedItemClass );

            itemBtn.userData = resultItem;
            iconBtn.userData = resultItem;
        }

        private void ResultsList_UnbindItem( VisualElement e, Int32 i )
        {
            e.RemoveFromClassList( Resources.SelectedItemClass );
        }

        private void ItemBtn_Clicked( ResultItem resultItem )
        {
            if ( !resultItem.IsNamespace )          //Select component, add component to GDObject
            {
                SelectType( resultItem );
            }
            else                                    //Show classes from selected namespace 
            {
                InspectNamespace( resultItem.Namespace );
            }
        }

        private void ResultsBtn_Clicked( )
        {
            //Back one namespace level                                                            
            if( _lastSearchResult.InspectNamespace.Any() )
            {
                ReturnInspectNamespace();
            }
        }

        private void IconBtn_Clicked( ResultItem resultItem )
        {
            //Add/remove item from Favorite list
            if ( !resultItem.IsNamespace )
            {
                _favorites.Remove( resultItem.Item.Type );
                if( !resultItem.IsFavorite )
                    _favorites.Insert( 0, resultItem.Item.Type );
                if ( _settings.SearchListMode == EListMode.Recommended )
                    ProcessRecommended();
                else
                    ProcessSearch();
                _settings.SaveFavoriteComponents( _favorites );
            }
        }


        private void SearchField_Changed(ChangeEvent<String> ev )
        {
            if( !String.Equals( ev.newValue, _lastSearchResult.SearchString ) )
                ProcessSearch( ev.newValue );
        }

        private void RecommendedToolBtn_Clicked( )
        {
            if( _settings.SearchListMode != EListMode.Recommended )
                ProcessRecommended();
        }

        private void SearchToolBtn_Clicked( )
        {
            if ( _settings.SearchListMode != EListMode.Search )
            {
                ProcessSearch(  );
                _searchField.Focus();
            }
        }

        private void Widget_KeyDown(KeyDownEvent evt )
        {
            if( _resultsToList.Count == 0 )                //All operations work on result items
                return;

            if ( evt.keyCode == KeyCode.DownArrow  )
            {
                SelectedViewItem = SelectedViewItem < 0 ? 0 : Math.Clamp( SelectedViewItem + 1, 0, _resultsToList.Count - 1 );
            }
            else if ( evt.keyCode == KeyCode.UpArrow  )
            {
                SelectedViewItem = SelectedViewItem < 0 ? _resultsToList.Count - 1 : Math.Clamp( SelectedViewItem - 1, 0, _resultsToList.Count - 1 );
            }
            else if ( evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter )
            {
                if ( SelectedViewItem >= 0 && SelectedViewItem < _resultsToList.Count )
                    SelectType( _resultsToList[ SelectedViewItem ] );
            }
            else if ( evt.keyCode == KeyCode.RightArrow )
            {
                if ( SelectedViewItem >= 0 && SelectedViewItem < _resultsToList.Count )
                {
                    var item = _resultsToList[ SelectedViewItem ];
                    if( item.IsNamespace )
                        InspectNamespace( item.Namespace );
                }
            }
            else if ( evt.keyCode == KeyCode.LeftArrow )
            {
                if ( SelectedViewItem >= 0 && SelectedViewItem < _resultsToList.Count )
                {
                    ReturnInspectNamespace();
                }
            }
        }

        private Int32 SelectedViewItem
        {
            get => _selectedViewItem;
            set
            {
                _resultsList.ScrollToItem( value );
                _selectedViewItem = value;
                UpdateSelectedItem();
            }
        }

        private void ProcessSearch( )
        {
            ProcessSearch( _lastSearchResult.SearchString );
        }

        private void ProcessSearch( String searchString )
        {
            _searchToolBtn.AddToClassList( Resources.SearchPopupBtnToggled );
            _recommendedToolBtn.RemoveFromClassList( Resources.SearchPopupBtnToggled );
            _settings.SearchListMode = EListMode.Search;
            
            _settings.LastSearchString = searchString;
            _lastSearchResult          = SearchItems( searchString );
            PrepareResults( _lastSearchResult );
            ShowResults( _lastSearchResult );
        }

        private void ProcessRecommended( )
        {
            _searchToolBtn.RemoveFromClassList( Resources.SearchPopupBtnToggled );
            _recommendedToolBtn.AddToClassList( Resources.SearchPopupBtnToggled );
            _settings.SearchListMode = EListMode.Recommended;

            _resultsLabel.text = "Recommended";
            var favoriteItems = new List<ResultItem>();
            foreach ( var favCompType in _favorites )
            {
                var favItem = _allTypes.First( i => i.Type == favCompType );
                favoriteItems.Add( new ResultItem()
                                   {
                                           Item = favItem,
                                           Label = favItem.ComponentName,
                                           Icon = Icons.FavoriteIcon,
                                           IsFavorite = true,
                                   } );
            }
            var mruItems = new List<ResultItem>();
            foreach ( var mruCompType in _mru )
            {
                var mruItem = _allTypes.First( i => i.Type == mruCompType );
                mruItems.Add( new ResultItem()
                              {
                                      Item  = mruItem,
                                      Label      = mruItem.ComponentName,
                                      Icon       = Icons.RecentIcon,
                              } );
            }

            _resultsToList.Clear();
            _resultsToList.AddRange( favoriteItems );
            _resultsToList.AddRange( mruItems );
            _resultsList.RefreshItems();
            SelectedViewItem = -1;
        }


        private Result SearchItems( String searchString )
        {
            if ( String.IsNullOrWhiteSpace( searchString ) )
                return new Result() { SearchString = searchString, Items = _allTypes.ToList() };
            else
                return new Result()
                       {
                               SearchString = searchString,
                               Items = _allTypes.Where( i =>
                                       i.ComponentName.Contains( searchString, StringComparison.OrdinalIgnoreCase ) ).ToList()
                       };
        }

        private void PrepareResults( Result result )
        {
            if ( result.InspectNamespace.Any() )
            {
                result.View = result.Items.Where( i => IsNamespaceBeginWith( i.Namespace, result.InspectNamespace ) )
                                    .Select( i => new ResultItem()
                                                  {
                                                          Item  = i, 
                                                          Label      = i.ComponentName, 
                                                          Namespace  = i.Namespace.Skip( result.InspectNamespace.Count ).ToList(),
                                                          Icon       = GetTypeIcon( i.Type ),
                                                          IsFavorite = _favorites.Contains( i.Type )
                                                  } ).ToList();
            }
            else
            {
                result.View = result.Items.Select( i => new ResultItem()
                                                        {
                                                                Item = i, 
                                                                Label = i.ComponentName, 
                                                                Namespace = i.Namespace,
                                                                Icon = GetTypeIcon( i.Type ),
                                                                IsFavorite = _favorites.Contains( i.Type )
                                                        } ).ToList();
            }

            CompactSearchResultView( result );

            result.View.Sort( (x, y) => String.Compare( x.Label, y.Label, StringComparison.Ordinal ) );

            //Highlight search string in component names
            for ( var i = 0; i < result.View.Count; i++ )
            {
                var resultItem = result.View[ i ];
                if ( !resultItem.IsNamespace )
                {
                    resultItem.RichLabel = resultItem.UseRichTextTags( result.SearchString, resultItem.Label );
                    result.View[ i ]         = resultItem;
                } 
            }
        }

        private Texture2D GetTypeIcon( Type type )
        {
            return _favorites.Contains( type ) 
                    ? Icons.FavoriteIcon 
                    : typeof(GDObject).IsAssignableFrom( type ) 
                            ? Icons.GDObjectIcon 
                            : typeof(ScriptableObject).IsAssignableFrom( type ) 
                                    ? Icons.SObjectIcon 
                                    : Resources.CSharpIcon;
        }

        private void CompactSearchResultView( Result result )
        {
            if ( result.View.Count > 20 )
            {
                var groupsByNamespace = result.View.Where( i => !i.IsNamespace && i.Namespace.Any() )
                                              .GroupBy( i => i.Namespace[ 0 ] )
                                              .OrderByDescending( g => g.Count() ).ToArray();

                foreach ( var mostCommonNamespace in groupsByNamespace )
                    if ( mostCommonNamespace.Count() > 1 )
                    {
                        var groupedItem   = new ResultItem()
                                            {
                                                    IsNamespace = true, 
                                                    Icon = Icons.NamespaceIcon,
                                                    SubitemsCount = mostCommonNamespace.Count(),
                                                    Label       = $"{mostCommonNamespace.Key} ({mostCommonNamespace.Count()})",
                                                    Namespace   = new List<String>(){ mostCommonNamespace.Key },
                                                    
                                            };
                        //Remove compacted classes, add one namespace fold
                        result.View.RemoveAll( i => !i.IsNamespace && i.Namespace.Any() && i.Namespace[ 0 ] == mostCommonNamespace.Key );
                        result.View.Add( groupedItem );

                        //Finished compacting
                        if ( result.View.Count <= 20 )
                            break;
                    }
                    else
                    {
                        //Cannot compact
                        return;
                    }
            }
        }

        private void ShowResults( Result result )
        {
            if ( result.InspectNamespace.Any() )
            {
                _resultsLabel.text = String.Join( ".", result.InspectNamespace );
                _backNamespaceIcon.style.display = DisplayStyle.Flex;
            }
            else
            {
                _resultsLabel.text               = $"Results: {result.Items.Count}";
                _backNamespaceIcon.style.display = DisplayStyle.None;
            }

            SelectedViewItem = -1;
            _resultsToList.Clear();
            _resultsToList.AddRange( result.View );
            _resultsList.Rebuild();
            if( _resultsToList.Count > 0 )
                _resultsList.ScrollToItem( 0 );
        }

        private Boolean IsNamespaceBeginWith( IReadOnlyList<String> ns, IReadOnlyList<String> search )
        {
            for ( var i = 0; i < search.Count; i++ )
            {
                if( ns.Count <= i )
                    return false;

                if ( !ns[ i ].Equals( search[ i ] ) )
                    return false;
            }

            return true;
        }

        private void UpdateSelectedItem( )
        {
            for ( var i = 0; i < _resultsToList.Count; i++ )
            {
                var resultItem = _resultsToList[ i ];
                resultItem.IsSelected = i == _selectedViewItem;
                _resultsToList[ i ] = resultItem;
            }
            _resultsList.RefreshItems();
        }

        private void SelectType( ResultItem item )
        {
            _mru.Remove( item.Item.Type );
            _mru.Insert( 0, item.Item.Type );
            _settings.SaveMRUComponents( _mru );
            SelectedType = item.Type;
            Selected?.Invoke( item.Type );
        }

        private void InspectNamespace( IReadOnlyList<String> ns )
        {
            _lastSearchResult.InspectNamespace.AddRange( ns );
            PrepareResults( _lastSearchResult );
            ShowResults( _lastSearchResult );
        }

        private void ReturnInspectNamespace(  )
        {
            if ( _lastSearchResult.InspectNamespace.Any() )
            {
                _lastSearchResult.InspectNamespace.RemoveAt( _lastSearchResult.InspectNamespace.Count - 1 );
                PrepareResults( _lastSearchResult );
                ShowResults( _lastSearchResult );
            }
        }

        /// <summary>
        /// Search result item for component type
        /// </summary>
        public struct Item : IEquatable<Item>
        {
            public IReadOnlyList<String> Namespace;
            public String       ComponentName;
            public Type         Type;

            public Boolean Equals(Item other)
            {
                return Type == other.Type;
            }

            public override bool Equals(object obj)
            {
                return obj is Item other && Equals( other );
            }

            public override int GetHashCode( )
            {
                return (Type != null ? Type.GetHashCode() : 0);
            }

            public static bool operator ==(Item left, Item right)
            {
                return left.Equals( right );
            }

            public static bool operator !=(Item left, Item right)
            {
                return !left.Equals( right );
            }
        }

        /// <summary>
        /// Search popup visual item  (type or namespace)
        /// </summary>
        public struct ResultItem
        {
            public  String  Label;
            public String RichLabel
            {
                get => String.IsNullOrEmpty( _labelWithTags ) ? Label : _labelWithTags;
                set => _labelWithTags = value;
            }
            public Texture2D Icon;
            public Boolean   IsSelected;

            //Namespace item
            public Boolean               IsNamespace;
            public Int32                 SubitemsCount;
            public IReadOnlyList<String> Namespace;              //Local to currently inspected namespace

            //Type item
            public Boolean IsFavorite;
            public Item    Item;
            public Type    Type => Item.Type;

            public String UseRichTextTags( String substring, String label )
            {
                if ( String.IsNullOrWhiteSpace( substring ) )
                    return label;
                else
                {
                    var pos = label.IndexOf( substring, StringComparison.OrdinalIgnoreCase );
                    if( pos >= 0 )
                        return String.Concat( label[ ..pos ], "<b>", label[ pos..( pos + substring.Length ) ], "</b>", label[ (pos + substring.Length).. ] );

                    return label;
                }
            }

            private String  _labelWithTags;
        }

        public class Result
        {
            public          String           SearchString;
            public          List<Item>       Items;
            public          List<ResultItem> View;
            public readonly List<String>     InspectNamespace = new();
        }

        public enum   EListMode
        {
             Search,
             Recommended
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset Window = UnityEngine.Resources.Load<VisualTreeAsset>( "TypeSearchWidget" );
            public static readonly VisualTreeAsset Item = UnityEngine.Resources.Load<VisualTreeAsset>( "TypeSearchItem" );

            public static readonly Texture2D CSharpIcon = UnityEngine.Resources.Load<Texture2D>( "tag_24dp" );
            public const  String SelectedItemClass     = "search-popup__item-selected";
            public const String SearchPopupBtnToggled = "search-popup__toolbtn-toggled";
        }
    }
}