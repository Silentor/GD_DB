using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GDDB.Editor.Validations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace GDDB.Editor
{
    /// <summary>
    /// Draws validation errors and some state icons in the project window
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowsEnchancer 
    {
        private static readonly Dictionary<Int32, ItemData> GdTypeCache = new ();
        //private static readonly GDObjectsFinder             GDOFinder;

        static ProjectWindowsEnchancer()
        {
            EditorApplication.projectWindowItemInstanceOnGUI += DrawIconsHandler;
            //EditorApplication.projectWindowItemOnGUI += DrawIcons2;

            GDObjectEditor.Changed += _ => InvalidateAndRepaint();                 //Unsaved changes in GDObject can change icons or validation state
            EditorDB.Updated       += InvalidateAndRepaint;                //React to DB changes
            Validator.Validated    += _ => InvalidateAndRepaint();
        }

        private static void InvalidateAndRepaint( )
        {
            GdTypeCache.Clear();
            EditorApplication.RepaintProjectWindow();
        }

        private static void DrawIconsHandler(Int32 instanceid, Rect rect )
        {
            const Single IconSize  = 18;

            if ( Event.current.type != EventType.Repaint || !IsMainListRect(rect) || EditorDB.DB == null )
                return;

            var iconRect = new Rect( rect.x + rect.width, rect.y, IconSize, IconSize );
            var itemData = GetItemData( instanceid );
            if( itemData.IsGDRootFolder )
            {
                //var oldColor = GUI.color;
                //GUI.color = Color.red;
                var content = new GUIContent( Icons.GDRootIcon, tooltip: "GD root folder" );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                //GUI.color = oldColor;
            }

            if( itemData.IsGDRootObject )
            {
                //var oldColor = GUI.color;
                var content = new GUIContent( Icons.GDRootIcon, tooltip: "GD root object" );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                //GUI.color = oldColor;
            }

            switch ( itemData.Disabled )
            {
                case EDisabledState.ObjectDisabledSelf:
                {
                    var content = new GUIContent( Resources.DisabledIcon, tooltip: "Object disabled, it's not included to database" );
                    iconRect.x -= IconSize;
                    GUI.Label( iconRect, content );
                } break;
                case EDisabledState.ObjectDisabledInFolder:
                {
                    var content = new GUIContent( Resources.DisabledIcon, tooltip: "Object disabled because some parent folder disabled" );
                    iconRect.x -= IconSize;
                    GUI.Label( iconRect, content );
                } break;
                case EDisabledState.FolderDisabledSelf:
                {
                    var content = new GUIContent( Resources.DisabledIcon, tooltip: "Folder disabled, all it content not included to database" );
                    iconRect.x -= IconSize;
                    GUI.Label( iconRect, content );
                } break;
                case EDisabledState.FolderDisabledInParent:
                {
                    var content = new GUIContent( Resources.DisabledIcon, tooltip: "Folder disabled because some parent folder disabled" );
                    iconRect.x -= IconSize;
                    GUI.Label( iconRect, content );
                }   break;
                case EDisabledState.Enabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if( itemData.InvalidGDObject )
            {
                //var oldColor = GUI.color;
                var content = new GUIContent( Icons.ErrorIcon, tooltip: itemData.InvalidGDOCustomTooltip );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                //GUI.color = oldColor;
            }
        }

        private static ItemData GetItemData( Int32 instanceId )
        {
            if( EditorDB.DB == null )
                return ItemData.Empty;

            if( GdTypeCache.TryGetValue( instanceId, out var itemData ) )
                return itemData;

            itemData = new ItemData();
            var asset       = EditorUtility.InstanceIDToObject( instanceId );
            if ( asset )
            {
                var editorDB = EditorDB.DB;
                itemData.DebugName = asset.name;
                if ( asset is DefaultAsset folderAsset )                      //Process folders
                {
                    var folderState = EditorDB.GetFolderState( folderAsset );
                    if ( folderState != EditorDB.EEnabledState.NotInGddb )                 //Skip ordinary folder outside Gddb
                    {
                        if ( folderState == EditorDB.EEnabledState.DisabledSelf )
                            itemData.Disabled = EDisabledState.FolderDisabledSelf;
                        else if ( folderState == EditorDB.EEnabledState.DisabledInParent )
                            itemData.Disabled = EDisabledState.FolderDisabledInParent;
                        else
                        {
                            var folderPath = AssetDatabase.GetAssetPath( folderAsset );
                            var folderGuid = Guid.ParseExact( AssetDatabase.AssetPathToGUID( folderPath ), "N" );
                            var gddbFolder = editorDB.GetFolder( folderGuid ); 
                            if ( gddbFolder == editorDB.RootFolder )
                                itemData.IsGDRootFolder = true;

                            if ( Validator.Reports.TryFirst( r => r.Folder.EnumeratePath().Any( f => f == gddbFolder ), out var report ) )       //todo optimize "folder is subfolder" comparison
                            {
                                itemData.InvalidGDObject         = true;
                                itemData.InvalidGDOCustomTooltip = "There are errors in GDObjects in this folder";
                            }
                        }
                    }
                }
                else if( asset is ScriptableObject so )                    //Process objects
                {
                    var objState = EditorDB.GetObjectState( so );
                    if ( objState != EditorDB.EEnabledState.NotInGddb )
                    {
                        if( objState == EditorDB.EEnabledState.DisabledSelf )
                            itemData.Disabled = EDisabledState.ObjectDisabledSelf;
                        else if( objState == EditorDB.EEnabledState.DisabledInParent )
                            itemData.Disabled = EDisabledState.ObjectDisabledInFolder;

                        if ( so is GDRoot )
                        {
                            itemData.IsGDRootObject = true;
                        }

                        if ( Validator.Reports.TryFirst( r => r.GdObject == so, out var report ))
                        {
                            itemData.InvalidGDObject         = true;
                            itemData.InvalidGDOCustomTooltip = report.Message;
                        }
                    }
                }
            }
            else
            {
                itemData.DebugName = "null";
            }

            GdTypeCache.Add( instanceId, itemData );
            return itemData;
        }

        private static bool IsMainListRect(Rect rect)
        {
            // Don't draw details if project view shows large preview icons:
            if (rect.height > 20)
            {
                return false;
            }
            // Don't draw details if this asset is a sub asset:
            // if (rect.x > 16)
            // {
            //     return false;
            // }

            return true;
        }

        private static class Resources
        {
            public static readonly GUIStyle GDTypeStrLabel = new (EditorStyles.label)
                                                             {
                                                                     alignment = TextAnchor.MiddleRight, 
                                                                     normal    = {textColor = Color.gray},
                                                                     hover     = {textColor = Color.gray},
                                                                     fontSize  = 11,
                                                                     padding = new RectOffset(0, 2, 0, 0),
                                                                     
                                                             };
            public static readonly GUIStyle GDTypeStrLabelSelected = new (EditorStyles.label)
                                                             {
                                                                     alignment = TextAnchor.MiddleRight, 
                                                                     normal    = {textColor = Color.white},
                                                                     hover     = {textColor = Color.white},
                                                                     fontSize  = 11,
                                                                     padding   = new RectOffset(0, 2, 0, 0),
                                                             };
            public static readonly GUIStyle GDTypeStrLabelError = new (EditorStyles.label)
                                                                     {
                                                                             alignment = TextAnchor.MiddleRight, 
                                                                             normal    = {textColor = Color.red},
                                                                             hover     = {textColor = Color.red},
                                                                             fontSize  = 11,
                                                                             padding   = new RectOffset(0, 2, 0, 0),
                                                                     };
            public static readonly GUIStyle GDTypeStrLabelDisabled = new (EditorStyles.label)
                                                                  {
                                                                          alignment = TextAnchor.MiddleRight, 
                                                                          normal    = {textColor = Color.gray},
                                                                          hover     = {textColor = Color.gray},
                                                                          fontSize  = 11,
                                                                          padding   = new RectOffset(0, 2, 0, 0),
                                                                          fontStyle = FontStyle.Italic
                                                                  };
            public static readonly GUIStyle GDTypeStrLabelDisabledSelected = new (EditorStyles.label)
                                                                     {
                                                                             alignment = TextAnchor.MiddleRight, 
                                                                             normal    = {textColor = Color.white},
                                                                             hover     = {textColor = Color.white},
                                                                             fontSize  = 11,
                                                                             padding   = new RectOffset(0, 2, 0, 0),
                                                                             fontStyle = FontStyle.Italic
                                                                     };

            
            public static readonly Texture2D DisabledIcon = UnityEngine.Resources.Load<Texture2D>( "visibility_off_24dp" );
        }

        [DebuggerDisplay("{DebugName}")]
        private struct ItemData
        {
            public Boolean        IsGDRootFolder;
            public Boolean        IsGDRootObject;
            public EDisabledState Disabled;
            public Boolean        InvalidGDObject;
            public String         InvalidGDOCustomTooltip;

            public String  DebugName;
            public String  GDTypeString;
            public Single  GDObjectNameWidth;
            public Single  GDTypeStrWidth;

            public static readonly ItemData Empty = new ItemData();
        }

        private enum EDisabledState
        {
            Enabled,
            ObjectDisabledSelf,
            ObjectDisabledInFolder,
            FolderDisabledSelf,
            FolderDisabledInParent
        }
    }
}

