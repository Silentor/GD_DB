using UnityEditor;
using UnityEngine;

namespace GDDB.Editor
{
    public class DataFileViewer : EditorWindow
    {
        [MenuItem( "MENUITEM/MENUITEMCOMMAND" )]
        private static void ShowWindow( )
        {
            var window = GetWindow<DataFileViewer>();
            window.titleContent = new GUIContent( "TITLE" );
            window.Show();
        }

        private void CreateGUI( )
        {
            
        }
    }
}