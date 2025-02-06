using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace GDDB.Editor
{
    /// <summary>
    /// Draws validation errors and some icons in the project window
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowsEnchancer 
    {
        private static readonly Dictionary<Int32, ItemData> GdTypeCache = new ();
        //private static readonly GDObjectsFinder             GDOFinder;

        static ProjectWindowsEnchancer()
        {
            //TypeHierarchy = new GDTypeHierarchy();
            //GDOFinder     = new GDObjectsFinder();

            EditorApplication.projectWindowItemInstanceOnGUI += DrawIconsHandler;
            //EditorApplication.projectWindowItemOnGUI += DrawIcons2;

            GDObjectEditor.Changed += GDObjectEditorOnChanged;
            GDAssets.GDDBAssetsChanged.Subscribe( 1000, GbbdStructureChanged );
        }

        private static void GbbdStructureChanged( IReadOnlyList<GDObject> changedObjects, IReadOnlyList<String> deletedObjects )
        {
            GdTypeCache.Clear();
        }

        private static void GDObjectEditorOnChanged( GDObject obj )
        {
            GdTypeCache.Clear();
            //GDOFinder.Reload();
        }

        private static void DrawIconsHandler(Int32 instanceid, Rect rect )
        {
            const Single IconSize  = 18;

            if ( Event.current.type != EventType.Repaint || !IsMainListRect(rect) )
                return;

            var iconRect = new Rect( rect.x + rect.width, rect.y, IconSize, IconSize );
            var itemData = GetItemData( instanceid );
            if( itemData.IsGDRootFolder )
            {
                var oldColor = GUI.color;
                //GUI.color = Color.red;
                var content = new GUIContent( Styles.GDRootIcon, tooltip: "GD root folder" );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                GUI.color = oldColor;
            }

            if( itemData.IsGDRootObject )
            {
                var oldColor = GUI.color;
                //GUI.color = Color.red;
                var content = new GUIContent( Styles.GDRootIcon, tooltip: "GD root object" );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                GUI.color = oldColor;
            }

            if( itemData.DisabledObject )
            {
                var oldColor = GUI.color;
                //GUI.color = Color.red;
                var content = new GUIContent( Styles.DisabledIcon, tooltip: "Object disabled, it is invisible for database" );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                GUI.color = oldColor;
            }

            if( itemData.InvalidGDObject )
            {
                var oldColor = GUI.color;
                //GUI.color = Color.red;
                var content = new GUIContent( Styles.InvalidGDO, tooltip: itemData.InvalidGDOCustomTooltip );
                iconRect.x -= IconSize;
                GUI.Label( iconRect, content );
                GUI.color = oldColor;
            }

            // var asset = EditorUtility.InstanceIDToObject(instanceid) as DefaultAsset;
            // if( asset )
            //     GUI.Label( rect, asset.name, Styles.GDTypeStrLabel );
            // else
            //     GUI.Label( rect, "null", Styles.GDTypeStrLabel );
            // //var asset = EditorUtility.InstanceIDToObject(instanceid) as GDObject;
            //if ( !asset ) return;

            //GUI.Label( rect, instanceid.ToString(), Styles.GDTypeStrLabel );

            //GUI.Label( rect, "test", Styles.GDTypeStrLabel );
            //GUI.DrawTexture( rect, EditorGUIUtility.whiteTexture );

            // var itemHash = HashCode.Combine( instanceid, asset.name.GetHashCode(), asset.Type.GetHashCode() );
            // if ( !GdTypeCache.TryGetValue( itemHash, out var itemData ) )
            // {
            //     itemData = new ItemData() { GDTypeString = TypeHierarchy.GetTypeString( asset.Type ),  } ;
            //     if( asset.Type != default )
            //         Styles.GDTypeStrLabel.CalcMinMaxWidth( new GUIContent( itemData.GDTypeString ), out _, out itemData.GDTypeStrWidth );
            //     GUI.skin.label.CalcMinMaxWidth( new GUIContent( asset.name ), out _, out var objNameWidth );
            //     itemData.GDObjectNameWidth = objNameWidth + 25;
            // }

            // if ( !asset.EnabledObject )
            // {
            //     if ( rect.width > itemData.GDObjectNameWidth + 50 )
            //     {
            //         if( Selection.objects.Contains( asset ) )
            //             GUI.Label( rect, "Disabled", Styles.GDTypeStrLabelDisabledSelected );
            //         else
            //             GUI.Label( rect, "Disabled", Styles.GDTypeStrLabelDisabled );
            //     }
            //     else if( rect.width > itemData.GDObjectNameWidth )
            //     {
            //         if( Selection.objects.Contains( asset ) )
            //             GUI.Label( rect, "x", Styles.GDTypeStrLabelDisabledSelected );
            //         else
            //             GUI.Label( rect, "x", Styles.GDTypeStrLabelDisabled );
            //     }
            //     return;
            // }

            // if( asset.Type == default )
            //     return;
            //
            // if( rect.width <= itemData.GDObjectNameWidth )
            //     return;
            //
            // var text = itemData.GDTypeString;
            // if ( rect.width < itemData.GDObjectNameWidth + itemData.GDTypeStrWidth )
            // {
            //     var pixelsPerChar = itemData.GDTypeStrWidth / itemData.GDTypeString.Length;
            //     var remainChars   = Mathf.Clamp( Mathf.RoundToInt( (rect.width - itemData.GDObjectNameWidth ) / pixelsPerChar ), 0, itemData.GDTypeString.Length );
            //     if( remainChars == 0 )
            //         return;
            //     else if ( remainChars < itemData.GDTypeString.Length )
            //         text = text.Substring( itemData.GDTypeString.Length - remainChars );
            // }

            //Validation
            // if( !TypeHierarchy.IsTypeDefined( asset.Type ) )
            //     GUI.Label( rect, new GUIContent( text, tooltip: "Type value is out of range"), Styles.GDTypeStrLabelError );
            // else if( !TypeHierarchy.IsTypeInRange( asset.Type, out var category ) )
            //     GUI.Label( rect, new GUIContent( text, tooltip: $"Type category {category.Index + 1} is out of range"), Styles.GDTypeStrLabelError );
            // else if( GDOFinder.IsDuplicatedType( asset.Type, TypeHierarchy, out var count ) )
            //     GUI.Label( rect, new GUIContent( text, tooltip: $"Duplicated type, found {count} types" ), Styles.GDTypeStrLabelError );
            // else
            // {
            //     if( Selection.objects.Contains( asset ) )
            //         GUI.Label( rect, text, Styles.GDTypeStrLabelSelected );
            //     else
            //         GUI.Label( rect, text, Styles.GDTypeStrLabel );
            // }
            
            
        }

        private static ItemData GetItemData( Int32 instanceId )
        {
            if( GdTypeCache.TryGetValue( instanceId, out var itemData ) )
                return itemData;

            itemData = new ItemData();
            var asset       = EditorUtility.InstanceIDToObject( instanceId );
            if ( asset )
            {
                var editorDB = GDBEditor.DB;
                if( editorDB.AllObjects.Count == 0 )
                {
                    itemData.DebugName = "GDDB is empty";
                    GdTypeCache.Add( instanceId, itemData );
                    return itemData;
                }

                itemData.DebugName = asset.name;
                var rootFolder  = GDBEditor.DB.RootFolder;
                if ( asset is DefaultAsset folderAsset )
                {
                    var folderGuid = new Guid( AssetDatabase.AssetPathToGUID( AssetDatabase.GetAssetPath( folderAsset ) ) );
                    if( folderGuid == rootFolder.FolderGuid )
                    {
                        itemData.IsGDRootFolder = true;
                    }

                    if ( Validator.Reports.TryFirst( r => r.Folder.EnumeratePath().Any( f => f.FolderGuid == folderGuid ), out var report ) )
                    {
                        itemData.InvalidGDObject         = true;
                        itemData.InvalidGDOCustomTooltip = "There are errors in GDObjects in this folder";
                    }
                }
                else if( asset is GDObject gdoAsset )
                {
                    if ( !gdoAsset.EnabledObject )
                    {
                        itemData.DisabledObject = true;
                    }
                    else
                    {
                        if ( gdoAsset is GDRoot )
                        {
                            itemData.IsGDRootObject = true;
                        }

                        if ( Validator.Reports.TryFirst( r => r.GdObject == gdoAsset, out var report ))
                        {
                            itemData.InvalidGDObject = true;
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

        private static class Styles
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

            public static readonly Texture2D GDRootIcon = Resources.Load<Texture2D>( "database_24dp" );
            public static readonly Texture2D DisabledIcon = Resources.Load<Texture2D>( "visibility_off_24dp" );
            public static readonly Texture2D InvalidGDO = Resources.Load<Texture2D>( "error_24dp" );


        }

        [DebuggerDisplay("{DebugName}")]
        private struct ItemData
        {
            public Boolean IsGDRootFolder;
            public Boolean IsGDRootObject;
            public Boolean DisabledObject;
            public Boolean InvalidGDObject;
            public String  InvalidGDOCustomTooltip;

            public String  DebugName;
            public String  GDTypeString;
            public Single  GDObjectNameWidth;
            public Single  GDTypeStrWidth;
        }
    }
}

