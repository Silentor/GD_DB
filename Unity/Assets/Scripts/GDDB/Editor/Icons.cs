using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    public static class Icons
    {
        public static readonly Texture2D GDObjectIcon = UnityEngine.Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "d_GDObject" : "GDObject");
        public static readonly Texture2D SObjectIcon = EditorGUIUtility.IconContent( EditorGUIUtility.isProSkin ? "d_ScriptableObject Icon" : "ScriptableObject Icon" ).image as Texture2D;
        public static readonly Texture2D NamespaceIcon = UnityEngine.Resources.Load<Texture2D>( "data_object_24dp" );
        public static readonly Texture2D FavoriteIcon  = UnityEngine.Resources.Load<Texture2D>( "star_24dp" );
        public static readonly Texture2D RecentIcon    = UnityEngine.Resources.Load<Texture2D>( "history_24dp" );
        public static readonly Texture2D GDRootIcon = Resources.Load<Texture2D>( "database_24dp" );
        public static readonly Texture2D ErrorIcon = Resources.Load<Texture2D>( "error_24dp" );
        public static readonly Texture2D FolderIcon = (Texture2D)EditorGUIUtility.IconContent( EditorGUIUtility.isProSkin ? "d_Folder Icon" : "Folder Icon" ).image;
    }
}