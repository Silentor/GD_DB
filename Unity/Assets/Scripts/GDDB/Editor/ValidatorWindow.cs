using System;
using System.Collections.Generic;
using System.Linq;
using Gddb.Editor.Validations;
using Gddb.Validations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Gddb.Editor
{
    public class ValidatorWindow : EditorWindow
    {
        private Button              _validateBtn;
        private MultiColumnListView _reports;
        private ValidationReport[]  _convertedReports;
        private Label               _summary;
        private ToolbarSearchField  _searchField;
        private bool             _reportsFiltered;

        public static void Open()
        {
            ValidatorWindow wnd = GetWindow<ValidatorWindow>();
            wnd.titleContent = new GUIContent("ValidatorWindow");
        }

        public void CreateGUI()
        {
            var           root        = rootVisualElement;
            VisualElement mainWindow = Resources.MainWindow.Instantiate();
            _validateBtn = mainWindow.Q<Button>( "ValidateBtn" );
            _validateBtn.clicked += ( ) =>
            {
                Validator.Validate();
            };
            Validator.Validated += ValidatorOnValidated;

            _reports = mainWindow.Q<MultiColumnListView>( "Reports" );
            _reports.columns[0].makeCell = () =>
            {
                var iconCell = new VisualElement();
                iconCell.AddToClassList( "reportsIcon" );
                return iconCell;
            };
            _reports.columns[0].bindCell   = (e, i) => { e.AddToClassList( "errorIcon" ); };
            _reports.columns[0].unbindCell = (e, i) => { e.RemoveFromClassList( "errorIcon" ); };
            _reports.columns[1].bindCell = (e, i) => { ((Label)e).text = _convertedReports[i].GetLocation(); };
            _reports.columns[2].bindCell = (e, i) => { ((Label)e).text = _convertedReports[i].Message; };
            _reports.itemsChosen += items =>
            {
                var report = (ValidationReport)items.FirstOrDefault();
                if( report != null && report.GdObject )
                    EditorGUIUtility.PingObject( report.GdObject );
            };
            
            _summary = mainWindow.Q<Label>( "Summary" );

            _searchField = mainWindow.Q<ToolbarSearchField>( "Search" );
            _searchField.RegisterValueChangedCallback( SearchFieldOnChanged );
                         
            ValidatorOnValidated( Validator.Reports );

            root.Add(mainWindow);
        }

        private void SearchFieldOnChanged(ChangeEvent<String> evt )
        {
            if ( String.IsNullOrEmpty( evt.newValue ) )
            {
                _reportsFiltered = false;
                ValidatorOnValidated( Validator.Reports );
            }
            else
            {
                var filteredReports = Validator.Reports.Where( r => r.GetLocation().Contains( evt.newValue, StringComparison.CurrentCultureIgnoreCase ) ).ToArray();
                _reportsFiltered = true;
                ValidatorOnValidated( filteredReports );
            }
        }

        private void ValidatorOnValidated(IReadOnlyList<ValidationReport> reports )
        {
            _convertedReports    = reports.ToArray();
            _reports.itemsSource = _convertedReports;
            _reports.RefreshItems();
            _summary.text = _reportsFiltered ? $"Filtered {_convertedReports.Length} of {Validator.Reports.Count} errors" : $"Found {_convertedReports.Length} errors";
        }

        private static class Resources
        {
            public static readonly VisualTreeAsset MainWindow = UnityEngine.Resources.Load<VisualTreeAsset>( "ValidatorWindow" );
            public static readonly Texture2D ErrorIcon = EditorGUIUtility.Load( "d_console.erroricon" ) as Texture2D;
        }
    }
}
