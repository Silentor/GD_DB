using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDDB.Editor
{
    /// <summary>
    /// Simple editor for folders inside gddb root folder for disabling folders
    /// </summary>
    [CustomEditor( typeof(DefaultAsset) )]
    public class FolderEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI( )
        {
            if ( EditorDB.DB == null )              //Do not override folder editor if no DB present
                return null;

            var folderPath = AssetDatabase.GetAssetPath( target );
            if ( AssetDatabase.TryGetAssetFolderInfo( folderPath, out _, out var isImmutable ) && !isImmutable )
            {
                if ( folderPath.StartsWith( EditorDB.RootFolderPath ) && folderPath != EditorDB.RootFolderPath )
                {
                    var editor  = new VisualElement();
                    var state   = EditorDB.GetFolderState( target );
                    var infoBox = new Box();
                    infoBox.style.flexDirection = FlexDirection.Row;
                    infoBox.style.alignItems  = Align.Center;
                    var icon    = new Image();
                    icon.image = Resources.InfoIcon;
                    infoBox.Add( icon );
                    var infoLabel = state switch
                                    {
                                            EditorDB.EEnabledState.DisabledSelf => new Label(
                                                    "Gddb folder is Disabled and not present in game data base with all content" ),
                                            EditorDB.EEnabledState.DisabledInParent => new Label(
                                                    "Gddb folder is Disabled because some parent folder is disabled and not present in game data base with all content" ),
                                            EditorDB.EEnabledState.Enabled => new Label(
                                                    "Gddb folder is Enabled. But can be disabled to temporary exclude it from game data base with all content" ),
                                            _ => throw new  ArgumentOutOfRangeException(),
                                    };
                    infoLabel.style.flexShrink = 1;
                    infoLabel.style.whiteSpace = WhiteSpace.Normal;
                    infoLabel.style.color      = new StyleColor( Color.gray );
                    infoBox.Add( infoLabel );
                    editor.Add( infoBox );
                    
                    var enabledToogle = new Toggle("Enabled");                              
                    editor.Add( enabledToogle );
                    enabledToogle.value = state != EditorDB.EEnabledState.DisabledSelf;
                    enabledToogle.RegisterValueChangedCallback( evt =>
                    {
                        SetEnabledState( target, evt.newValue );
                    } );
                    return editor;
                }
            }
        
            return null;
        }

        public static event Action Updated;             //When some folder enabled state changed


        private void SetEnabledState( UnityEngine.Object folderAsset, Boolean state )
        {
            var labels = AssetDatabase.GetLabels( folderAsset );
            if( state && labels.Contains( GdDbAssetsParser.GddbFolderDisabledLabel ) )
            {
                labels = labels.Where( l => l != GdDbAssetsParser.GddbFolderDisabledLabel ).ToArray();
                AssetDatabase.SetLabels( folderAsset, labels );  
                Updated?.Invoke();
            }
            else if( !state && !labels.Contains( GdDbAssetsParser.GddbFolderDisabledLabel ) )
            {
                labels = labels.Append( GdDbAssetsParser.GddbFolderDisabledLabel ).ToArray();
                AssetDatabase.SetLabels( folderAsset, labels );
                Updated?.Invoke();
            }
        }

        private static class Resources
        {
            public static readonly Texture InfoIcon = EditorGUIUtility.IconContent( "console.infoicon" ).image;
        }

        
    }
}