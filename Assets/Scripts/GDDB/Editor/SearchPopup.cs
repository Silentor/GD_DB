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
        public SearchPopup( GDObjectEditor editor, SerializedProperty componentsProp, Settings settings )
        {
            _editor         = editor;
            _componentsProp = componentsProp;
            _settings       = settings;
        }

        public IReadOnlyList<Item> AllComponents => _allComponents;

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

            var asset    = UnityEngine.Resources.Load<VisualTreeAsset>( "SearchPopup" );
            var instance = asset.Instantiate();
            editorWindow.rootVisualElement.Add( instance );

            _itemAsset = UnityEngine.Resources.Load<VisualTreeAsset>( "SearchPopupItem" );

            _searchToolBtn = instance.Q<Button>( "SearchBtn" );
            _searchToolBtn.clicked += SearchToolBtnOnclicked;
            _recommendedToolBtn = instance.Q<Button>( "RecommendedBtn" );
            _recommendedToolBtn.clicked += RecommendedToolBtnOnclicked;

            var searchField = instance.Q<TextField>( "SearchField" );
            searchField.RegisterValueChangedCallback( SearchFieldOnChanged );
            searchField.RegisterCallback<AttachToPanelEvent>( _ => searchField.Focus() );

            _resultsLabel         =  instance.Q<Label>( "ResultsLbl" );
            _backNamespaceIcon    =  instance.Q<VisualElement>( "BackNamespaceIcon" );
            _resultsBtn           =  instance.Q<Button>( "ResultsBtn" );
            _resultsBtn.clicked += ResultsBtn_Clicked;
            _resultsBtn.RemoveFromClassList( "unity-button" );

            _resultsList             = instance.Q<ListView>( "ResultsList" );
            _resultsList.makeItem    = ResultsList_MakeItem;
            _resultsList.bindItem    = ResultsList_BindItem;
            _resultsList.itemsSource = _resultsToList;

            //Prepare all components cache
            var allComponentTypes   = TypeCache.GetTypesDerivedFrom( typeof(GDComponent) );
            var allProperComponents = allComponentTypes.Where( t => !t.IsAbstract ).OrderBy( t => t.Name ).ToArray();
            _allComponents = allProperComponents.Select( t => new Item
                                                              {
                                                                      Namespace     = t.Namespace == null ? Array.Empty<String>() : t.Namespace.Split( '.' ).ToList(),
                                                                      ComponentName = t.Name,
                                                                      Type          = t
                                                              } ).ToList();

            //Restore prev state
            var types = new List<Type>();
            _settings.LoadMRUComponents( types );
            _mru.AddRange( _allComponents.Where( c => types.Contains( c.Type ) ) );             
            _settings.LoadFavoriteComponents( types );
            _favorites.AddRange( _allComponents.Where( c => types.Contains( c.Type ) ) );

            _lastSearchResult = new Result() { SearchString = _settings.LastSearchString, Items = _allComponents.ToList() };
            searchField.value = _settings.LastSearchString;

            if( _settings.SearchListMode == EListMode.Recommended )
                ProcessRecommended();
            else
                ProcessSearch();
        }

        public override void OnClose( )
        {
            base.OnClose();

            _settings.SaveMRUComponents( _mru.Select( c => c.Type ).ToArray() );
            _settings.SaveFavoriteComponents( _favorites.Select( c => c.Type ).ToArray() );
        }

        private readonly GDObjectEditor      _editor;
        private readonly SerializedProperty  _componentsProp;
        private readonly List<ResultItem>    _resultsToList = new ();
        private          ListView            _resultsList;
        private          IReadOnlyList<Item> _allComponents;
        private          VisualTreeAsset     _itemAsset;
        private          Label               _resultsLabel;
        private          Result              _lastSearchResult;
        private          VisualElement       _backNamespaceIcon;
        private          Button              _resultsBtn;
        private          Button              _searchToolBtn;
        private          Button              _recommendedToolBtn;
        private readonly Settings            _settings;

        private readonly List<Item> _mru = new();
        private readonly List<Item> _favorites = new();

        private VisualElement ResultsList_MakeItem( )
        {
            var result = _itemAsset.Instantiate();
            var itemBtn    = result.Q<Button>( "ItemBtn" );
            itemBtn.clicked += ( ) => OnItemClicked( (ResultItem) itemBtn.userData );
            var iconBtn = result.Q<Button>( "IconBtn" );
            iconBtn.clicked += ( ) => OnIconClicked( (ResultItem) iconBtn.userData );
            return result;
        }

       

        private void ResultsList_BindItem( VisualElement e, Int32 i )
        {
            var resultItem = _resultsToList[ i ];
            var itemBtn    = e.Q<Button>( "ItemBtn" );
            var iconBtn    = e.Q<Button>( "IconBtn" );
            iconBtn.style.backgroundImage = resultItem.Icon;
            var label      = itemBtn.Q<Label>( "Label" );
            label.text    = resultItem.LabelWithTags;
            
            if ( !resultItem.IsNamespace )
            {
                label.tooltip = String.Join( ".", resultItem.Component.Namespace.Append( resultItem.Component.ComponentName ) );
                e.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = DisplayStyle.None;
            }
            else
            {
                label.tooltip  = String.Join( ".", _lastSearchResult.InspectNamespace.Append( resultItem.Label ) );
                e.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = StyleKeyword.Null;
            }

            itemBtn.userData = resultItem;
            iconBtn.userData = resultItem;
        }

        private void OnItemClicked( ResultItem resultItem )
        {
            if ( !resultItem.IsNamespace )          //Select component, add component to GDObject
            {
                _editor.AddComponent( _componentsProp, resultItem.Component.Type );
                _mru.Remove( resultItem.Component );
                _mru.Insert( 0, resultItem.Component );
                editorWindow.Close();
            }
            else                                    //Show classes from selected namespace 
            {
                _lastSearchResult.InspectNamespace.AddRange( resultItem.Namespace );
                PrepareSearchResults( _lastSearchResult );
                ShowResults( _lastSearchResult );
            }
        }

        private void OnIconClicked( ResultItem resultItem )
        {
            //Add/remove item from Favorite list
            if ( !resultItem.IsNamespace )
            {
                _favorites.Remove( resultItem.Component );
                if( !resultItem.IsFavorite )
                    _favorites.Insert( 0, resultItem.Component );
                if ( _settings.SearchListMode == EListMode.Recommended )
                    ProcessRecommended();
                else
                    ProcessSearch();
            }
        }


        private void SearchFieldOnChanged(ChangeEvent<String> ev )
        {
            ProcessSearch( ev.newValue );
        }

        private void RecommendedToolBtnOnclicked( )
        {
            ProcessRecommended();
        }

        private void SearchToolBtnOnclicked( )
        {
            ProcessSearch(  );
        }

        private void ProcessSearch( )
        {
            ProcessSearch( _lastSearchResult.SearchString );
        }

        private void ProcessSearch( String searchString )
        {
            _searchToolBtn.AddToClassList( "search-popup__toolbtn-toggled" );
            _recommendedToolBtn.RemoveFromClassList( "search-popup__toolbtn-toggled" );
            _settings.SearchListMode = EListMode.Search;

            if ( !String.Equals( searchString, _lastSearchResult.SearchString ) )
            {
                _settings.LastSearchString = searchString;
                _lastSearchResult          = SearchItems( searchString );
            }
            
            PrepareSearchResults( _lastSearchResult );
            ShowResults( _lastSearchResult );
        }

        private void ProcessRecommended( )
        {
            _searchToolBtn.RemoveFromClassList( "search-popup__toolbtn-toggled" );
            _recommendedToolBtn.AddToClassList( "search-popup__toolbtn-toggled" );
            _settings.SearchListMode = EListMode.Recommended;

            _resultsLabel.text = "Recommended";
            var favoriteItems = _favorites.Take( _settings.MaxFavoriteViewItems )
                                          .Select( i => new ResultItem()
                                             {
                                                     Component = i, 
                                                     Label     = i.ComponentName,
                                                     Icon      = Resources.FavoriteIcon,
                                                     IsFavorite = true,
                                             } ).ToList();
             var mruItems = _mru.Where( i => favoriteItems.All( f => f.Type != i.Type ) )
                                .Take( _settings.MaxMRUViewItems )
                                .Select( i => new ResultItem()
                                 {
                                         Component = i, 
                                         Label = i.ComponentName,
                                         Icon = Resources.RecentIcon,
                                 } ).ToList();

             _resultsToList.Clear();
             _resultsToList.AddRange( favoriteItems );
             _resultsToList.AddRange( mruItems );
             _resultsList.RefreshItems();
        }


        private Result SearchItems( String searchString )
        {
            if ( String.IsNullOrWhiteSpace( searchString ) )
                return new Result() { SearchString = searchString, Items = _allComponents.ToList() };
            else
                return new Result()
                       {
                               SearchString = searchString,
                               Items = _allComponents.Where( i =>
                                       i.ComponentName.Contains( searchString, StringComparison.OrdinalIgnoreCase ) ).ToList()
                       };
        }

        private void PrepareSearchResults( Result result )
        {
            if ( result.InspectNamespace.Any() )
            {
                result.View = result.Items.Where( i => NamespaceBeginWith( i.Namespace, result.InspectNamespace ) )
                                    .Select( i => new ResultItem()
                                                  {
                                                          Component = i, 
                                                          Label = i.ComponentName, 
                                                          Namespace = i.Namespace.Skip( result.InspectNamespace.Count ).ToList(),
                                                          Icon = _favorites.Contains( i ) ? Resources.FavoriteIcon : Resources.CSharpIcon,
                                                  } ).ToList();
            }
            else
            {
                result.View = result.Items.Select( i => new ResultItem()
                                                        {
                                                                Component = i, 
                                                                Label = i.ComponentName, 
                                                                Namespace = i.Namespace,
                                                                Icon = _favorites.Contains( i ) ? Resources.FavoriteIcon : Resources.CSharpIcon,
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
                    resultItem.LabelWithTags = resultItem.UseRichTextTags( result.SearchString, resultItem.Label );
                    result.View[ i ]         = resultItem;
                } 
            }
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
                                                    Icon = Resources.NamespaceIcon,
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

            _resultsToList.Clear();
            _resultsToList.AddRange( result.View );
            _resultsList.RefreshItems();
        }

        private Boolean NamespaceBeginWith( IReadOnlyList<String> ns, IReadOnlyList<String> search )
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

        private void ResultsBtn_Clicked( )
        {
            //Back one namespace level````````````````````                                                                  
            if( _lastSearchResult != null && _lastSearchResult.InspectNamespace.Any() )
            {
                _lastSearchResult.InspectNamespace.RemoveAt( _lastSearchResult.InspectNamespace.Count - 1 );
                PrepareSearchResults( _lastSearchResult );
                ShowResults( _lastSearchResult );
            }
        }


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

        public struct ResultItem
        {
            public  String  Label;
            public String LabelWithTags
            {
                get => String.IsNullOrEmpty( _labelWithTags ) ? Label : _labelWithTags;
                set => _labelWithTags = value;
            }
            public Texture2D Icon;

            //Namespace mode
            public Boolean               IsNamespace;
            public Int32                 SubitemsCount;
            public IReadOnlyList<String> Namespace;

            //Component mode
            public Boolean IsFavorite;
            public Item    Component;
            public Type    Type => Component.Type;

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

        public enum EListMode
        {
             Search,
             Recommended
        }

        public static class Resources
        {
            public static Texture2D CSharpIcon = UnityEngine.Resources.Load<Texture2D>( "tag_24dp" );
            public static Texture2D NamespaceIcon = UnityEngine.Resources.Load<Texture2D>( "list_alt_24dp" );
            public static Texture2D FavoriteIcon = UnityEngine.Resources.Load<Texture2D>( "star_24dp" );
            public static Texture2D RecentIcon = UnityEngine.Resources.Load<Texture2D>( "history_24dp" );
        }
    }
}