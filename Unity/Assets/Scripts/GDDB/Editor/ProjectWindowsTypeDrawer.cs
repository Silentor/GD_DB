using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    [InitializeOnLoad]
    public static class ProjectWindowsTypeDrawer 
    {
        private static readonly GDTypeHierarchy             TypeHierarchy;
        private static readonly Dictionary<Int32, ItemData> GdTypeCache = new ();
        //private static readonly GDObjectsFinder             GDOFinder;

        static ProjectWindowsTypeDrawer()
        {
            TypeHierarchy = new GDTypeHierarchy();
            //GDOFinder     = new GDObjectsFinder();

            //EditorApplication.projectWindowItemInstanceOnGUI += DrawGDTypeString;
            GDObjectEditor.Changed += GDObjectEditorOnChanged;
        }

        private static void GDObjectEditorOnChanged( GDObject obj )
        {
            GdTypeCache.Clear();
            //GDOFinder.Reload();
        }

        private static void DrawGDTypeString(Int32 instanceid, Rect rect )
        {
            if ( Event.current.type != EventType.Repaint || !IsMainListRect(rect) )
                return;

            var asset = EditorUtility.InstanceIDToObject(instanceid) as GDObject;
            if ( !asset )
                return;


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

        private static bool IsMainListRect(Rect rect)
        {
            // Don't draw details if project view shows large preview icons:
            if (rect.height > 20)
            {
                return false;
            }
            // Don't draw details if this asset is a sub asset:
            if (rect.x > 16)
            {
                return false;
            }

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


        }

        private struct ItemData
        {
            public String GDTypeString;
            public Single GDObjectNameWidth;
            public Single GDTypeStrWidth;
        }
    }
}

