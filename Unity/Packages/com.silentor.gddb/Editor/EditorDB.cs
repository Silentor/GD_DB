using System;
using System.Collections.Generic;
using System.Linq;
using Gddb.Serialization;
using UnityEditor;
using UnityEngine;

namespace Gddb.Editor
{
    /// <summary>
    /// Access to cached Gddb for editor purposes
    /// </summary>
    [InitializeOnLoad]
    public class EditorDB
    {
        static EditorDB( )
        {
            AssetsWatcher.GddbAssetsChanged += OnGddbStructureChanged;      //React to changes in GDObjects assets
            FolderEditor.Updated               += UpdateState;
            UpdateState();
        }

        public static IReadOnlyList<ScriptableObject> AllObjects => _allObjects;

        public static IReadOnlyList<GdFolder> AllFolders => _allFolders;

        public static IReadOnlyList<String> DisabledFolders => _disabledFolders;

        public static String RootFolderPath => _rootFolderPath;

        public static Int32 DisabledObjectsCount => _disabledObjectsCount;

        /// <summary>
        /// Null if no Gddb found
        /// </summary>
        public static GdDb DB => _gbd;

        public static Guid GetGDObjectGuid( ScriptableObject obj )
        {
            if( obj is GDObject gdObject )
                return gdObject.Guid;
            else if( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( obj, out var guid, out long _ ) )
                return Guid.ParseExact( guid, "N" );
            else
                return Guid.Empty;
        }

        public static EEnabledState GetFolderState( UnityEngine.Object folderAsset )
        {
            if ( DB == null )
                return EEnabledState.NotInGddb;

            var path   = AssetDatabase.GetAssetPath( folderAsset );
            if ( !path.StartsWith( RootFolderPath, StringComparison.Ordinal ) )
                return EEnabledState.NotInGddb;

            var labels = AssetDatabase.GetLabels( folderAsset );
            if( Array.IndexOf( labels, GdDbAssetsParser.GddbFolderDisabledLabel ) >= 0 )
                return EEnabledState.DisabledSelf;
            else
            {
                
                foreach ( var disabledFolder in DisabledFolders )
                {
                    if ( path.StartsWith( disabledFolder ) )
                        return EEnabledState.DisabledInParent;
                }
            }

            return EEnabledState.Enabled;
        }

        public static EEnabledState GetObjectState( ScriptableObject objectAsset )
        {
            if ( DB == null )
                return EEnabledState.NotInGddb;

            var path   = AssetDatabase.GetAssetPath( objectAsset );
            if ( !path.StartsWith( RootFolderPath, StringComparison.Ordinal ) )
                return EEnabledState.NotInGddb;

            if ( objectAsset is GDObject gdo && !gdo.EnabledObject )
                return EEnabledState.DisabledSelf;
            else
            {
                foreach ( var disabledFolder in DisabledFolders )
                {
                    if ( path.StartsWith( disabledFolder ) )
                        return EEnabledState.DisabledInParent;
                }
            }

            return EEnabledState.Enabled;
        }

        /// <summary>
        /// Editor GDDB was updated due to changed GDObjects assets (or folders)
        /// </summary>
        public static event Action Updated;

        public enum EEnabledState
        {
            Enabled,
            DisabledSelf,
            DisabledInParent,
            NotInGddb
        }

        private static GdDb                            _gbd;
        private static IReadOnlyList<ScriptableObject> _allObjects;
        private static IReadOnlyList<GdFolder>         _allFolders;
        private static IReadOnlyList<String>           _disabledFolders;
        private static String                          _rootFolderPath;
        private static Int32                             _disabledObjectsCount;

        private static void UpdateState( )
        {
            Debug.Log( $"[{nameof(EditorDB)}]-[{nameof(UpdateState)}] updating editor Gddb" );

            var loader = new GdEditorLoader();
            _gbd               = loader.GetGameDataBase();
            _allObjects        = loader.AllObjects?.Select( obj => obj.Object ).ToList();
            _allFolders        = loader.AllFolders;
            _disabledFolders   = loader.DisabledFolders;
            _rootFolderPath    = loader.RootFolderPath;
            _disabledObjectsCount = loader.DisabledObjectsCount;

            Updated?.Invoke();
        }

        private static void OnGddbStructureChanged( )
        {
            UpdateState();
        }
    }
}