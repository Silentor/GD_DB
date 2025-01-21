using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GDDB.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BinaryReader = GDDB.Serialization.BinaryReader;
using Object = System.Object;

namespace GDDB.Editor
{
    public class DataFileViewer : EditorWindow
    {
        private          ListView       _content;
        private readonly List<ItemData> _data = new List<ItemData>();

        

        private void CreateGUI( )
        {
            var window = UnityEngine.Resources.Load<VisualTreeAsset>( "DataFileViewer" ).Instantiate();
            var openBinBtn = window.Q<Button>( "OpenBinBtn" );
            openBinBtn.clicked += OpenBinFile;
            var openJsonBtn = window.Q<Button>( "OpenJsonBtn" );
            openJsonBtn.clicked += OpenJsonFile;

            _content          =  window.Q<ListView>( "Content" );
            _content.fixedItemHeight = 20;
            _content.makeItem += Resources.ContentItemPrefab.Instantiate;
            _content.bindItem += BindItem;
            _content.itemsSource = _data;

            rootVisualElement.Add( window );
        }


        private void BindItem( VisualElement itemWidget, Int32 itemIndex )
        {
            var item = _data[ itemIndex ];
            itemWidget.Q<Label>( "Index" ).text = itemIndex.ToString();
            itemWidget.Q<Label>( "Indent" ).text = GetIndent( item.Depth );
            itemWidget.Q<Label>( "Token" ).text = item.Token;
            itemWidget.Q<Label>( "Value" ).text = item.ValueAsString;
        }

        private String GetIndent( Int32 depth )
        {
            return depth switch
            {
                0 => String.Empty,
                1 => "  ",
                2 => "    ",
                3 => "      ",
                4 => "        ",
                _ => new String( ' ', depth * 2 )
            };
        }

        private void OpenBinFile( )
        {
            var defaultFolder = "Assets";
            if( AssetDatabase.IsValidFolder( "Assets/StreamingAssets" ) )
                defaultFolder = "Assets/StreamingAssets";
            else if( AssetDatabase.IsValidFolder( "Assets/Resources" ) )
                defaultFolder = "Assets/Resources";

            var filePath = EditorUtility.OpenFilePanel( "Open bin file", defaultFolder, "bin" );
            if ( System.IO.File.Exists( filePath ) )
            {
                using var fileStream = new FileStream( filePath, FileMode.Open, FileAccess.Read );
                var reader = new BinaryReader( fileStream );
                var data = LoadFile( reader );
                UpdateContent( data );
            }
        }

        private void OpenJsonFile( )
        {
            var defaultFolder = "Assets";
            if( AssetDatabase.IsValidFolder( "Assets/StreamingAssets" ) )
                defaultFolder = "Assets/StreamingAssets";
            else if( AssetDatabase.IsValidFolder( "Assets/Resources" ) )
                defaultFolder = "Assets/Resources";

            var filePath = EditorUtility.OpenFilePanel( "Open json file", defaultFolder, "json" );
            if ( System.IO.File.Exists( filePath ) )
            {
                using var fileStream = new FileStream( filePath, FileMode.Open, FileAccess.Read );
                var reader = new JsonNetReader( new StreamReader( fileStream ) );
                var data = LoadFile( reader );
                UpdateContent( data );
            }
        }


        private List<ItemData> LoadFile( ReaderBase reader )
        {
            var index  = 0;
            var data   = new List<ItemData>();
            while( reader.ReadNextToken() != EToken.EoF )
            {
                String value = String.Empty;
                String token = reader.CurrentToken.ToString();
                switch ( reader.CurrentToken )
                {
                    case EToken.Int8:
                        token  = "Int8";
                        value = reader.GetIntegerValue().ToString();
                        break;
                    case EToken.UInt8:
                    case EToken.Int16:
                    case EToken.UInt16:
                    case EToken.Int32:
                    case EToken.UInt32:
                    case EToken.Int64:
                        value = reader.GetIntegerValue().ToString();
                        break;
                    case EToken.UInt64:
                        value = reader.GetUInt64Value().ToString();
                        break;

                    case EToken.String:
                        value = reader.GetStringValue();
                        break;

                    case EToken.PropertyName:
                        value = reader.GetPropertyName();
                        break;

                    case EToken.Single:
                        value = reader.GetSingleValue().ToString( CultureInfo.InvariantCulture );
                        break;

                    case EToken.Double:
                        value = reader.GetDoubleValue().ToString( CultureInfo.InvariantCulture );
                        break;

                    case EToken.Guid:
                        value = reader.GetGuidValue().ToString();
                        break;

                    case EToken.False:  
                        //value = "False";
                        break;

                    case EToken.True:
                        //value = "True";
                        break;

                    case EToken.Null:
                        token = "Null";
                        //value = "Null";
                        break;

                    case EToken.StartObject:
                        token = "StartObject";
                        break;

                    default:
                        if ( reader.CurrentToken.IsAliasToken() )
                        {
                            var aliasIndex = (Byte)reader.CurrentToken & 0x7F; //Get the alias index
                            token = "Alias";
                            value = $"{aliasIndex}";
                            break;
                        }

                        break;

                }
                data.Add(  
                        new ItemData
                        {
                                Index = index++, 
                                Token = token, 
                                ValueAsString = value,
                                Depth = reader.Depth
                        } );
            }

            Debug.Log( $"[{nameof(DataFileViewer)}]-[{nameof(LoadFile)}] Loaded {data.Count} tokens" );

            return data;
        }

        private void UpdateContent( List<ItemData> data )
        {
            _data.Clear();
            _data.AddRange( data );
             _content.Rebuild();
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset ContentItemPrefab = UnityEngine.Resources.Load<VisualTreeAsset>( "DataFileViewerItem" );
        }

        private struct ItemData
        {
            public Int32  Index;
            public String Token;
            public String ValueAsString;
            public Int32  Depth;
        }
    }
}