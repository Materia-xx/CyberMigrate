using DataProvider;
using Dto;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Tasks.BuiltIn.FeatureDependency
{
    /// <summary>
    /// Interaction logic for FeatureDependencyUC.xaml
    /// </summary>
    public partial class FeatureDependencyChooseFeatureUC : UserControl
    {
        public int SelectedFeatureId { get; private set; }

        public int SelectedSystemStateId { get; private set; }

        /// <summary>
        /// Indicates if the user confirmed the choice or if they cancelled out of the selection
        /// </summary>
        public bool SelectionConfirmed { get; private set; }

        private ObservableCollection<CMSystemDto> ComboBox_Systems = new ObservableCollection<CMSystemDto>();

        private ObservableCollection<CMFeatureDto> ComboBox_Features = new ObservableCollection<CMFeatureDto>();

        private ObservableCollection<CMSystemStateDto> ComboBox_States = new ObservableCollection<CMSystemStateDto>();

        private Window ParentWindow { get; set; }

        /// <summary>
        /// Indicates if the UI should show feature templates or feature instances
        /// </summary>
        private bool ShowTemplates { get; set; }

        public FeatureDependencyChooseFeatureUC(int initialFeatureId, int initialSystemStateId, bool showTemplates, Window parentWindow)
        {
            InitializeComponent();

            SelectedFeatureId = initialFeatureId;
            SelectedSystemStateId = initialSystemStateId;
            ShowTemplates = showTemplates;
            ParentWindow = parentWindow;

            ComboBox_Systems.Clear();
            var cmSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();
            foreach (var combobox_cmSystem in cmSystems)
            {
                ComboBox_Systems.Add(combobox_cmSystem);
            }

            cboSystem.ItemsSource = ComboBox_Systems;
            cboSystem.DisplayMemberPath = nameof(CMSystemDto.Name);
            cboSystem.SelectedValuePath = nameof(CMSystemDto.Id);

            cboFeature.ItemsSource = ComboBox_Features;
            cboFeature.DisplayMemberPath = nameof(CMFeatureDto.Name);
            cboFeature.SelectedValuePath = nameof(CMFeatureDto.Id);

            cboState.ItemsSource = ComboBox_States;
            cboState.DisplayMemberPath = nameof(CMSystemStateDto.Name);
            cboState.SelectedValuePath = nameof(CMSystemStateDto.Id);
        }

        private void ReloadComboBox_Features()
        {
            ComboBox_Features.Clear();
            var selectedSystemObj = cboSystem.SelectedItem;
            if (selectedSystemObj == null)
            {
                return;
            }
            var selectedSystem = selectedSystemObj as CMSystemDto;
            var cmFeatures = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_ForSystem(selectedSystem.Id, ShowTemplates);
            foreach (var cmFeature in cmFeatures)
            {
                ComboBox_Features.Add(cmFeature);
            }
        }

        private void ReloadComboBox_States()
        {
            ComboBox_States.Clear();
            var selectedFeatureObj = cboFeature.SelectedItem;
            if (selectedFeatureObj == null)
            {
                return;
            }
            var selectedFeature = selectedFeatureObj as CMFeatureDto;

            // If this is a feature template then get the system states directly for it, otherwise get them for the parent feature template
            var featureTemplateId = selectedFeature.CMParentFeatureTemplateId == 0 ? selectedFeature.Id : selectedFeature.CMParentFeatureTemplateId;
            var cmStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(featureTemplateId);
            foreach (var cmState in cmStates)
            {
                ComboBox_States.Add(cmState);
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedSystemStateId != 0)
            {
                try
                {
                    var selectedFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(SelectedFeatureId);

                    cboSystem.SelectedItem = ComboBox_Systems.First(s => s.Id == selectedFeature.CMSystemId);
                    ReloadComboBox_Features();
                    cboFeature.SelectedItem = ComboBox_Features.First(f => f.Id == SelectedFeatureId);
                    ReloadComboBox_States();
                    cboState.SelectedItem = ComboBox_States.First(s => s.Id == SelectedSystemStateId);
                }
                catch
                {
                    MessageBox.Show("The values that were set on this dependency previously cannot be represented within the current configuration. Please re-set the values.");
                }
            }

            cboSystem.SelectionChanged += CboSystem_SelectionChanged;
            cboFeature.SelectionChanged += CboFeature_SelectionChanged;
            cboState.SelectionChanged += CboState_SelectionChanged;
        }

        private void CboSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReloadComboBox_Features();
        }

        private void CboFeature_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReloadComboBox_States();
        }

        private void CboState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Updating the state is the only thing that will insert or update the task data
            var selectedFeatureObj = cboFeature.SelectedItem;
            if (selectedFeatureObj == null)
            {
                return;
            }
            var selectedFeature = selectedFeatureObj as CMFeatureDto;

            var selectedStateObj = cboState.SelectedItem;
            if (selectedStateObj == null)
            {
                return;
            }
            var selectedState = selectedStateObj as CMSystemStateDto;

            SelectedFeatureId = selectedFeature.Id;
            SelectedSystemStateId = selectedState.Id;
        }

        private void btnChooseFeature_Click(object sender, RoutedEventArgs e)
        {
            SelectionConfirmed = true;
            ParentWindow.Close();
        }
    }
}
