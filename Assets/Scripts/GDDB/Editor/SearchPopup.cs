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

            var asset    = Resources.Load<VisualTreeAsset>( "SearchPopup" );
            var instance = asset.Instantiate();
            editorWindow.rootVisualElement.Add( instance );

            _itemAsset = Resources.Load<VisualTreeAsset>( "SearchPopupItem" );

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
            _settings.LoadMRUComponents( _allComponents, _mrus );
            _lastSearchResult = new Result() { SearchString = _settings.LastSearchString, Items = _allComponents.ToList() };
            searchField.value = _settings.LastSearchString;

            if( _settings.SearchListMode == EListMode.Recommended )
                RecommendedToolBtnOnclicked();
            else
                SearchToolBtnOnclicked();
        }

        public override void OnClose( )
        {
            base.OnClose();

            _settings.SaveMRUComponents( _mrus );
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
        private readonly List<SearchPopup.Item> _mrus = new();

        private VisualElement ResultsList_MakeItem( )
        {
            var result = _itemAsset.Instantiate();
            var btn    = result.Q<Button>( "ItemBtn" );
            btn.clicked += ( ) => OnItemClicked( (ResultItem) btn.userData );
            btn.RemoveFromClassList( "unity-button" );
            return result;
        }

        private void ResultsList_BindItem( VisualElement e, Int32 i )
        {
            var resultItem = _resultsToList[ i ];
            var itemBtn    = e.Q<Button>( "ItemBtn" );
            var label      = itemBtn.Q<Label>( "Label" );
            label.text    = resultItem.LabelWithTags;
            if ( !resultItem.IsNamespace )
            {
                label.tooltip = String.Join( ".", resultItem.Component.Namespace.Append( resultItem.Component.ComponentName ) );
                itemBtn.Q<VisualElement>( "NamespaceIcon" ).style.display = DisplayStyle.None;
                itemBtn.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = DisplayStyle.None;
            }
            else
            {
                label.tooltip  = String.Join( ".", _lastSearchResult.InspectNamespace.Append( resultItem.Label ) );
                itemBtn.Q<VisualElement>( "NamespaceIcon" ).style.display     = StyleKeyword.Null;
                itemBtn.Q<VisualElement>( "OpenNamespaceIcon" ).style.display = StyleKeyword.Null;
            }

            itemBtn.userData = resultItem;
        }

        private void OnItemClicked( ResultItem resultItem )
        {
            if ( !resultItem.IsNamespace )
            {
                _editor.AddComponent( _componentsProp, resultItem.Component.Type );
                _mrus.Insert( 0, resultItem.Component );
                editorWindow.Close();
            }
            else            //Show selected namespace classes
            {
                _lastSearchResult.InspectNamespace.AddRange( resultItem.Namespace );
                PrepareResults( _lastSearchResult );
                ShowResults( _lastSearchResult );
            }
        }


        private void SearchFieldOnChanged(ChangeEvent<String> ev )
        {
            ProcessSearch( ev.newValue );
        }

        private void RecommendedToolBtnOnclicked( )
        {
            _searchToolBtn.RemoveFromClassList( "search-popup__toolbtn-toggled" );
            _recommendedToolBtn.AddToClassList( "search-popup__toolbtn-toggled" );
            _settings.SearchListMode = EListMode.Recommended;
            ProcessRecommended();
        }

        private void SearchToolBtnOnclicked( )
        {
            _searchToolBtn.AddToClassList( "search-popup__toolbtn-toggled" );
            _recommendedToolBtn.RemoveFromClassList( "search-popup__toolbtn-toggled" );
            _settings.SearchListMode = EListMode.Search;
            ProcessSearch( _lastSearchResult.SearchString );
        }

        private void ProcessSearch( String searchString )
        {
            _settings.LastSearchString = searchString;
            _lastSearchResult = SearchItems( searchString );
            PrepareResults( _lastSearchResult );
            ShowResults( _lastSearchResult );
        }

        private void ProcessRecommended( )
        {
            _resultsLabel.text = "Recommended";
             var mruResultItems = _mrus.Take( _settings.MaxMRUItems ).Select( i => new ResultItem()
                                                  {
                                                          Component = i, 
                                                          Label = i.ComponentName
                                                  } ).ToList();
             _resultsToList.Clear();
             _resultsToList.AddRange( mruResultItems );
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

        private void PrepareResults( Result result )
        {
            if ( result.InspectNamespace.Any() )
            {
                result.View = result.Items.Where( i => NamespaceBeginWith( i.Namespace, result.InspectNamespace ) )
                                    .Select( i => new ResultItem()
                                                  {
                                                          Component = i, 
                                                          Label = i.ComponentName, 
                                                          Namespace = i.Namespace.Skip( result.InspectNamespace.Count ).ToList()
                                                  } ).ToList();
            }
            else
            {
                result.View = result.Items.Select( i => new ResultItem()
                                                        {
                                                                Component = i, 
                                                                Label = i.ComponentName, 
                                                                Namespace = i.Namespace
                                                        } ).ToList();
            }

            CompactResultView( result );

            result.View.Sort( (x, y) => String.Compare( x.Label, y.Label, StringComparison.Ordinal ) );
        }

        private void CompactResultView( Result result )
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
                                                    SubitemsCount = mostCommonNamespace.Count(),
                                                    Label       = $"{mostCommonNamespace.Key} ({mostCommonNamespace.Count()})",
                                                    Namespace   = new List<String>(){ mostCommonNamespace.Key },
                                                    
                                            };
                        result.View.RemoveAll( i =>
                                !i.IsNamespace && i.Namespace.Any() && i.Namespace[ 0 ] == mostCommonNamespace.Key );
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
                PrepareResults( _lastSearchResult );
                ShowResults( _lastSearchResult );
            }
        }


        public struct Item
        {
            public IReadOnlyList<String> Namespace;
            public String       ComponentName;
            public Type         Type;
        }

        public struct ResultItem
        {
            public  String  Label;
            public String LabelWithTags
            {
                get => String.IsNullOrEmpty( _labelWithTags ) ? Label : _labelWithTags;
                set => _labelWithTags = value;
            }
            public  Boolean IsNamespace;

            //Namespace mode
            public Int32   SubitemsCount;
            public IReadOnlyList<String> Namespace;

            //Component mode
            public Item    Component;

            public String UseRichTextTags( String substring, String label )
            {
                if ( String.IsNullOrWhiteSpace( substring ) )
                    return label;
                else
                {
                    var pos = label.IndexOf( substring, StringComparison.OrdinalIgnoreCase );
                    try
                    {
                        return String.Concat( label[ ..pos ], "<b>", label[ pos..( pos + substring.Length ) ], "</b>", label[ (pos + substring.Length).. ] );
                    }
                    catch ( Exception e )
                    {
                        Debug.LogException( e );
                        Debug.Log( $"label {label}, subs {substring}" );
                        throw;
                    }
                    
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
    }
}