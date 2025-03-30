using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    public class TypeSearchSettings
    {
        public Int32 MaxMRUViewItems                = 10;
        public Int32 MaxMRUStoreItems               = 20;
        public Int32 MaxFavoriteViewItems           = 10;
        public Int32 MaxFavoriteStoreItems          = 20;


        public TypeSearchSettings( String key )
        {
            key ??= "";
            _settingsPrefix = $"{Application.identifier}.GDDB.Editor.TypeSearchSettings.{key}.";
            _mruKey = $"{_settingsPrefix}MRUComponents";
            _favKey = $"{_settingsPrefix}FavoriteComponents";
            _listModeKey = $"{_settingsPrefix}SearchListMode";
            _lastSearchStringKey = $"{_settingsPrefix}LastSearchString";
        }

        public String LastSearchString
        {
            get => EditorPrefs.GetString( _lastSearchStringKey, "" );
            set => EditorPrefs.SetString( _lastSearchStringKey, value );
        }

        public TypeSearchWidget.EListMode SearchListMode
        {
            get => (TypeSearchWidget.EListMode)EditorPrefs.GetInt( _listModeKey, 0 );
            set => EditorPrefs.SetInt( _listModeKey, (Int32)value );
        }

        public void SaveMRUComponents( IReadOnlyList<Type> components )
        {
            SaveComponentsList( components, MaxMRUStoreItems, _mruKey );
        } 

        public void LoadMRUComponents( [NotNull] List<Type> result )
        {
            if ( result == null ) throw new ArgumentNullException( nameof(result) );

            LoadComponentsList( _mruKey, MaxMRUStoreItems, result );
        } 

        public void SaveFavoriteComponents( IReadOnlyList<Type> components )
        {
            SaveComponentsList( components, MaxFavoriteStoreItems, _favKey );
        } 

        public void LoadFavoriteComponents( [NotNull] List<Type> result )
        {
            if ( result == null ) throw new ArgumentNullException( nameof(result) );

            LoadComponentsList( _favKey, MaxFavoriteStoreItems, result );
        } 

        private readonly String _settingsPrefix;
        private readonly String _mruKey;
        private readonly String _favKey;
        private readonly string _listModeKey;
        private readonly string _lastSearchStringKey;

        private void LoadComponentsList( String storeKey, Int32 maxCount, [NotNull] List<Type> result )
        {
            if ( result == null ) throw new ArgumentNullException( nameof(result) );

            result.Clear();
            var serializedStr = EditorPrefs.GetString( storeKey, null );
            if( String.IsNullOrEmpty( serializedStr ) )
                return;

            var items = JsonUtility.FromJson<ItemsWrapper>( serializedStr );
            foreach ( var typeFullName in items.Items.Take( maxCount ) )
            {
                var componentType = Type.GetType( typeFullName, false );
                if( componentType != null )
                    result.Add( componentType );
            }
        } 

        public void SaveComponentsList( IReadOnlyList<Type> components, Int32 maxStoreCount, String storeKey )
        {
            var items = new String[ Math.Min( maxStoreCount, components.Count ) ];
            for ( int i = 0; i < items.Length; i++ )
            {
                items[ i ] = components[ i ].AssemblyQualifiedName;
            }
            var serializedStr = JsonUtility.ToJson( new ItemsWrapper {Items = items} );
            EditorPrefs.SetString( storeKey, serializedStr );
        } 

        private class ItemsWrapper
        {
            public String[] Items;
        }
    }
}