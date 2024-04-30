using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    [InitializeOnLoad]
    public static class ProjectWindowDetails 
    {
        static ProjectWindowDetails()
        {
            EditorApplication.projectWindowItemOnGUI += DrawAssetDetails;
        }

        private static void DrawAssetDetails(string guid, Rect rect)
        {
            if (Application.isPlaying || Event.current.type != EventType.Repaint || !IsMainListAsset(rect))
            {
                return;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }
            var asset = AssetDatabase.LoadAssetAtPath<GDObject>(assetPath);
            if ( !asset )
            {
                // this entry could be Favourites or Packages. Ignore it.
                return;
            }

            var gdtype = asset.Type;

            // Right align label:
            const int width = 250;
            rect.x     += rect.width - width;
            rect.width =  width;
            GUI.Label(rect, guid);
        }

        private static bool IsMainListAsset(Rect rect)
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
    }
}

