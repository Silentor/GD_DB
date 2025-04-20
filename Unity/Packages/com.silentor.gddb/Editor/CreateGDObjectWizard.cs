using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;
using PopupWindow = UnityEditor.PopupWindow;

namespace Gddb.Editor
{
    /// <summary>
    /// Very simple wizard to select a GDObject type and create an instance of it in current folder in Project window.
    /// </summary>
    public class CreateGDObjectWizard : EditorWindow
    {
        private Type _gdObjectType;
        private Button _createBtn;
        private Label _infoLabel;
        private static readonly MethodInfo _tryGetActiveFolderPathMethod = typeof(ProjectWindowUtil).GetMethod( "TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic );
        private static readonly IReadOnlyList<Type> _propertTypes = GetProperGDObjectTypes();

        public void CreateGUI()
        {
            var root = rootVisualElement;
            var window = LocalResources.MainWindow.Instantiate();
            window.style.flexGrow = 1;
            var gdObjectTypeSelectContainer = window.Q<VisualElement>("GDObject");
            var typeSearchWidget = new TypeSearchWidget( new TypeSearchSettings( nameof(ScriptableObject) ), _propertTypes );
            typeSearchWidget.Selected += SetGDObjectType;
            gdObjectTypeSelectContainer.Add( typeSearchWidget.CreateGUI() );

            _createBtn = window.Q<Button>("CreateBtn");
            _createBtn.clicked += CreateGdObject;
            _infoLabel = window.Q<Label>("InfoLabel");

            Selection.selectionChanged += ( ) => SetGDObjectType( _gdObjectType );

            SetGDObjectType( _gdObjectType );

            root.Add(window);
        }

        private void Update( )
        {
            RefreshState();
        }

        private void RefreshState( )
        {
            var typeName = _gdObjectType != null ? _gdObjectType.Name : "GDObject";
            var pathName = TryGetActiveFolderPath( out var path ) ? path : "Assets/";
            var isValidPath = AssetDatabase.TryGetAssetFolderInfo( pathName, out var isRoot, out bool isImmutable ) && !isImmutable;
            _infoLabel.text = isValidPath ? $"Create new {typeName} at {pathName}" : $"Incorrect path {pathName} for creating GDObject";
            _createBtn.SetEnabled( isValidPath );
        }

        private static IReadOnlyList<Type> GetProperGDObjectTypes( )
        {
            //We need only user defined scriptable objects
            var unityDataPath = EditorApplication.applicationContentsPath.Replace( '/', Path.DirectorySeparatorChar );          //To work with Assembly.Location
            var types            = TypeCache.GetTypesDerivedFrom<ScriptableObject>().Where( t => !t.IsAbstract && !t.Assembly.Location.StartsWith( unityDataPath ) && !t.Assembly.FullName.StartsWith( "Unity" ) && (t.Namespace == null || (!t.Namespace.StartsWith( "Unity" ))) ).ToArray();
            return types;
        }


        private void CreateGdObject( )
        {
            var outputFolder = "Assets/";
            if ( TryGetActiveFolderPath( out var selectedFolder ) )
                outputFolder = selectedFolder;

            var newObject = GDObject.CreateInstance( _gdObjectType );
            AssetDatabase.CreateAsset( newObject, outputFolder );
            Close();
        }

        private void SetGDObjectType( Type type )
        {
            _gdObjectType = type;
        }

        private static bool TryGetActiveFolderPath( out string path )
        {
            object[] args = new Object[] { null };
            bool found = (bool)_tryGetActiveFolderPathMethod.Invoke( null, args );
            path = (string)args[0];

            return found;
        }

        private static class LocalResources
        {
            public static readonly VisualTreeAsset MainWindow = UnityEngine.Resources.Load<VisualTreeAsset>("CreateGDObjectWizard");
            
        }
    }
}
