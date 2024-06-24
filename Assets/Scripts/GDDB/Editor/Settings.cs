using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    public class Settings
    {
        public Int32 MaxMRUItems => 10;

        public Settings( )
        {
            _settingsPrefix = $"GDDB.Editor.Settings.{Application.identifier}.";
            _mruKey = $"{_settingsPrefix}MRUComponents";
            _listModeKey = $"{_settingsPrefix}SearchListMode";
            _lastSearchStringKey = $"{_settingsPrefix}LastSearchString";
        }

        public String LastSearchString
        {
            get => EditorPrefs.GetString( _lastSearchStringKey, "" );
            set => EditorPrefs.SetString( _lastSearchStringKey, value );
        }

        public SearchPopup.EListMode SearchListMode
        {
            get => (SearchPopup.EListMode)EditorPrefs.GetInt( _listModeKey, 0 );
            set => EditorPrefs.SetInt( _listModeKey, (Int32)value );
        }

        public void SaveMRUComponents( IReadOnlyList<SearchPopup.Item> items )
        {
            var mru = new String[ Math.Min( MaxMRUItems, items.Count ) ];
            for ( int i = 0; i < mru.Length; i++ )
            {
                mru[i] = items[i].Namespace.Any() 
                        ? String.Concat( String.Join( ".", items[i].Namespace ), ".", items[i].ComponentName ) 
                        : items[i].ComponentName;
            }
            var serializedStr = JsonUtility.ToJson( new ItemsWrapper {Items = mru} );
            EditorPrefs.SetString( _mruKey, serializedStr );
        } 

        public void LoadMRUComponents( IReadOnlyList<SearchPopup.Item> allItems, [NotNull] List<SearchPopup.Item> result )
        {
            if ( result == null ) throw new ArgumentNullException( nameof(result) );

            result.Clear();
            var serializedStr = EditorPrefs.GetString( _mruKey, null );
            if( String.IsNullOrEmpty( serializedStr ) )
                return;

            var mru = JsonUtility.FromJson<ItemsWrapper>( serializedStr ).Items;
            foreach ( var itemStr in mru.Take( MaxMRUItems ) )
            {
                var nsAndName = itemStr.Split( ".", StringSplitOptions.RemoveEmptyEntries );
                if( nsAndName.Length == 0 )
                    continue;

                foreach ( var item in allItems )
                {
                    if ( IsEqual( item, nsAndName ) )
                    {
                        result.Add( item );
                        break;
                    }
                }
            }

            return;

            static Boolean IsEqual( SearchPopup.Item item, String[] ns_name )
            {
                if ( ns_name.Length == 0 )
                    return false;
                var name = ns_name[ ^1 ];

                if ( item.ComponentName ==  name && ns_name.Length - 1 == item.Namespace.Count )
                {
                    for ( int i = 0; i < item.Namespace.Count; i++ )
                    {
                        if ( item.Namespace[ i ] != ns_name[ i ] )
                        {
                            return false;
                        }
                    }
                    return true;
                }

                return false;
            }
        } 

        private readonly String _settingsPrefix;
        private readonly String _mruKey;
        private readonly string _listModeKey;
        private readonly string _lastSearchStringKey;

        private class ItemsWrapper
        {
            public String[] Items;
        }
    }
}