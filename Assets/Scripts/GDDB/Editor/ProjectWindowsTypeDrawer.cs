using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace GDDB.Editor
{
    [InitializeOnLoad]
    public static class ProjectWindowsTypeDrawer 
    {
        private static readonly GDTypeHierarchy TypeHierarchy;
        private static readonly Dictionary<Int32, String> GdTypeStrCache = new ();

        static ProjectWindowsTypeDrawer()
        {
            EditorApplication.projectWindowItemInstanceOnGUI += DrawGDTypeString;
            GDObjectEditor.Changed += GDObjectEditorOnChanged;
            TypeHierarchy = new GDTypeHierarchy();
        }

        private static void GDObjectEditorOnChanged( GDObject obj )
        {
            GdTypeStrCache.Clear();
        }

        private static void DrawGDTypeString(Int32 instanceid, Rect rect )
        {
            if ( Event.current.type != EventType.Repaint || !IsMainListRect(rect) )
                return;

            var asset = EditorUtility.InstanceIDToObject(instanceid) as GDObject;
            if ( !asset )
                return;

            if( asset.Type == default )
                return;

            // Right align label:
            const int width = 250;
            rect.x     += rect.width - width;
            rect.width =  width;

            if ( !GdTypeStrCache.TryGetValue( instanceid, out var gdTypeStr ) )
            {
                gdTypeStr = TypeHierarchy.GetTypeString( asset.Type );
                GdTypeStrCache.Add( instanceid, gdTypeStr );
            }

            if( Selection.activeObject == asset )
                GUI.Label( rect, gdTypeStr, Styles.GDTypeStrLabelSelected );
            else
                GUI.Label( rect, gdTypeStr, Styles.GDTypeStrLabel );
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

        }
    }
}

