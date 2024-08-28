using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using Debug = UnityEngine.Debug;
using GDDB;
using UnityEngine;

namespace GDDB.Editor
{
    /// <summary>
    /// Provides a tree of Categories for GdType
    /// </summary>
    public class GDTypeHierarchy
    {
        private readonly List<Category> _flatList;
        private readonly StringBuilder              _stringBuilder = new ();        //For GetTypeString

        public readonly  Category       Root;

        public  GDTypeHierarchy( )
        {
            var startTimer = new Stopwatch();

            var treeJsonFiles = AssetDatabase.FindAssets( "TreeStructure" );
            if( treeJsonFiles.Length == 0 )
                return;

            String json = null;
            foreach ( var jsonFileId in treeJsonFiles )
            {
                var path = AssetDatabase.GUIDToAssetPath( jsonFileId );
                if ( path.EndsWith( "TreeStructure.json" ) )
                {
                    var    treeJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>( path );
                    json = treeJsonFile.text;
                    break;
                }
            }

            if( json == null )
                return;

            var parser       = new TreeStructureParser();
            var rootCategory = parser.ParseJson( json, CancellationToken.None );
            Root = rootCategory;

            startTimer.Stop();

            _flatList = new List<Category>();
            parser.ToFlatList( rootCategory, _flatList );
            var possibleTypesCount = 0;
            foreach ( var category in _flatList )
            {
                if ( category.Type == CategoryType.Enum )
                    possibleTypesCount += category.Items.Count( i => i.Subcategory == null );
                else
                    possibleTypesCount += category.Count;
            }
            Debug.Log( $"[GDTypeHierarchy] Hierrarchy build time {startTimer.ElapsedMilliseconds} ms, possible types count {possibleTypesCount}" );
            PrintHierarchy();
        }

        public String GetTypeString( GdType type )
        {
            if ( type == default )
                return "None";

            _stringBuilder.Clear();
            GetTypeString( type, _stringBuilder );
            return _stringBuilder.ToString();
        }

        /// <summary>
        /// For given type check if every component is in proper category range
        /// </summary>
        /// <param name="type"></param>
        /// <param name="incorrectIndex"></param>
        /// <returns></returns>
        public Boolean IsTypeInRange( GdType type, out Category incorrectCategory )
        {
            if ( type == default )
            {
                incorrectCategory = null;
                return true;
            }

            var metadata = GetMetadataOf( type );
            return metadata.IsTypeInRange( type, out incorrectCategory );
        }

        public Boolean IsTypeDefined( GdType type )
        {
            if ( type == default )
                return true;

            var metadata = GetMetadataOf( type );
            return metadata.IsTypeDefined( type );
        }

        public GdTypeMetadata GetMetadataOf( GdType type )
        {
            if ( type == default )
                return GdTypeMetadata.Default;

            if ( Root == null )
            {
                return new GdTypeMetadata( new List<Category>() );
            }

            var result   = new List<Category>( 4 );
            var category = Root;
            for ( int i = 0; i < 4; i++ )
            {
                if ( category != null )
                {
                    result.Add( category );
                    var categoryItem = category.GetItem( category.GetValue(type.Data) );
                    category    = categoryItem?.Subcategory;
                }
                else break;
            }

            return new GdTypeMetadata( result );
        }

        [MenuItem( "GDDB/Print hierarchy" )]
        private static void PrintHierarchyToConsole( )
        {
             var instance = new GDTypeHierarchy();
             Debug.Log( instance.PrintHierarchy() );
        }

        public String PrintHierarchy( )
        {
            if( Root == null )
                return "No categories found";

            _stringBuilder.Clear();
            _stringBuilder.Append( Root.Name );
            _stringBuilder.Append( " (" );
            _stringBuilder.Append( Root.Type );
            _stringBuilder.AppendLine( ") :" );
            PrintCategoryRecursive( Root, _stringBuilder, 0 );
            return _stringBuilder.ToString();
        }

        private void PrintCategoryRecursive( Category category, StringBuilder result, Int32 depth )
        {
            if ( category == null || category.Items == null )
                return;

            foreach ( var item in category.Items )
            {
                result.Append( ' ', depth * 4 );
                result.Append( category.Name );
                result.Append( "." );
                result.Append(  item.Name );
                if ( item.Subcategory != null )
                {
                    result.Append( " (" );
                    result.Append( item.Subcategory.Type );
                    result.AppendLine( ") :" );
                    PrintCategoryRecursive( item.Subcategory, result, depth + 1 );
                }
                else
                    result.AppendLine();
            }
        }

        private void GetTypeString( GdType type, StringBuilder result )
        {
            var metadata = GetMetadataOf( type );
            for ( var i = 0; i < metadata.Categories.Count; i++ )
            {
                var category = metadata.Categories[ i ];
                var value    = category.GetValue( type );
                var categoryItem = category.GetItem( value );
                result.Append( categoryItem != null ? categoryItem.Name : value.ToString( CultureInfo.InvariantCulture ) );
                if ( i < metadata.Categories.Count - 1 )
                    result.Append( '.' );    
            }
        }

        public class GdTypeMetadata
        {
            /// <summary>
            /// Metadata of default GdType
            /// </summary>
            public static readonly GdTypeMetadata Default = new ( new List<Category>() );

            public IReadOnlyList<Category> Categories => _categories;

            public UInt32 Mask
            {
                get
                {
                    if ( _mask == null )
                    {
                        _mask = 0;
                        for ( var i = 0; i < _categories.Length; i++ )
                        {
                            var category = _categories[ i ];
                            _mask |= category.GetMask() << Category.GetShift( i, category.Type );
                        }
                    }

                    return _mask.Value;
                }
            }

            public GdTypeMetadata( List<Category> categories )
            {
                _categories = categories.ToArray();
            }

            public Boolean IsTypeDefined( GdType type )
            {
                if ( type == default )
                    return true;

                return (type.Data & Mask)  == type.Data;
            }

            public GdType ClearUndefinedTypePart( GdType type )
            {
                return GdType.CreateFromRawData( type.Data & Mask );
            }
        

        public Boolean IsTypeInRange( GdType type, out Category incorrectCategory )
            {
                if( type == default )
                {
                    incorrectCategory = null;
                    return true;
                }

                var data = type.Data;
                for ( var i = 0; i < _categories.Length; i++ )
                {
                    var category = _categories[ i ];
                    if ( !category.IsCorrectValue( category.GetValue( data ) ) )
                    {
                        incorrectCategory = category;
                        return false;
                    }
                }

                incorrectCategory = null;
                return true;
            }

            public Boolean IsTypesEqual( GdType type1, GdType type2 )
            {
                if( type1 == default && type2 == default )
                    return true;

                return (type1.Data & Mask) == (type2.Data & Mask);
            }

            private readonly Category[] _categories;
            private          UInt32?                _mask;
        }
    }
}