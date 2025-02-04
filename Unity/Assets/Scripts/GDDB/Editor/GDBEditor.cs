﻿using System;
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
    public class GDBEditor
    {
        private static GdDb                    _gbd;
        private static Boolean                 _isGDAssetsChanged = true;
        private static IReadOnlyList<GDObject> _allObjects;
        private static IReadOnlyList<Folder> _allFolders;

        static GDBEditor( )
        {
            GDAssets.GDDBAssetsChanged.Subscribe( -1000, OnGddbStructureChanged );     //Update editor GDDB instance, so it need to be first call
        }

        public static IReadOnlyList<GDObject> AllObjects
        {
            get
            {
                UpdateState();
                return _allObjects;
            }
        }

        public static IReadOnlyList<Folder> AllFolders
        {
            get
            {
                UpdateState();
                return _allFolders;
            }
        }

        public static GdDb GDB
        {
            get
            {
                UpdateState();
                return _gbd;
            }
        }

        private static void UpdateState( )
        {
            if( _gbd == null || _isGDAssetsChanged )
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
            Debug.Log( $"[{nameof(GDBEditor)}]-[{nameof(OnGddbStructureChanged)}] Editor GDB instance will be recreated due to changed GDObjects assets " );
            _isGDAssetsChanged = true;
        }
    }
}