using System;
using System.Collections.Generic;
using GDDB.Serialization;
using UnityEditor;
using UnityEngine;
using GDDB;

namespace GDDB.Editor
{
    /// <summary>
    /// Editor static access to cached GDB
    /// </summary>
    [InitializeOnLoad]
    public class EditorDB
    {

        static EditorDB( )
        {
            GDAssetProcessor.GDDBAssetsChanged.Subscribe( -1000, OnGddbStructureChanged );     //Update editor GDDB instance, so it need to be first call
        }

        public static IReadOnlyList<ScriptableObject> AllObjects
        {
            get
            {
                UpdateState();
                return _allObjects;
            }
        }

        public static IReadOnlyList<GdFolder> AllFolders
        {
            get
            {
                UpdateState();
                return _allFolders;
            }
        }

        public static GdDb DB
        {
            get
            {
                UpdateState();
                return _gbd;
            }
        }

        public static Guid GetGDObjectGuid( ScriptableObject obj )
        {
            if( obj is GDObject gdObject )
                return gdObject.Guid;
            else if( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( obj, out var guid, out long _ ) )
                return Guid.ParseExact( guid, "N" );
            else
                return Guid.Empty;
        }

        public static event Action Updated;

        private static GdDb                            _gbd;
        private static Boolean                         _isGDAssetsChanged = true;
        private static IReadOnlyList<ScriptableObject> _allObjects;
        private static IReadOnlyList<GdFolder>         _allFolders;

        private static void UpdateState( )
        {
            if( _isGDAssetsChanged )
            {
                var loader = new GdEditorLoader();
                _gbd               = loader.GetGameDataBase();
                _allObjects        = loader.AllObjects;
                _allFolders        = loader.AllFolders;
                _isGDAssetsChanged = false;
            }
        }

        private static void OnGddbStructureChanged(IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> deletedObjects )
        {
            Debug.Log( $"[{nameof(EditorDB)}]-[{nameof(OnGddbStructureChanged)}] Editor GDB instance will be recreated due to changed GDObjects assets " );
            _isGDAssetsChanged = true;
            Updated?.Invoke();
        }
    }
}