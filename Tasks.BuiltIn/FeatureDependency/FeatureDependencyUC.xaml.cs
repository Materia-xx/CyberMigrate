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
    public partial class FeatureDependencyUC : UserControl
    {
        private int cmSystemId;
        private int cmFeatureId;
        private int cmTaskId;

        private CMTaskDto cmTask;

        private FeatureDependencyDto TaskData;

        private ObservableCollection<CMSystemDto> ComboBox_Systems = new ObservableCollection<CMSystemDto>();

        private ObservableCollection<CMFeatureDto> ComboBox_Features = new ObservableCollection<CMFeatureDto>();

        private ObservableCollection<CMSystemStateDto> ComboBox_States = new ObservableCollection<CMSystemStateDto>();

        public FeatureDependencyUC(int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            InitializeComponent();

            this.cmSystemId = cmSystemId;
            this.cmFeatureId = cmFeatureId;
            this.cmTaskId = cmTaskId;

            cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmTaskId);

            ComboBox_Systems.Clear();
            var cmSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();
            foreach (var cmSystem in cmSystems)
            {
                ComboBox_Systems.Add(cmSystem);
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

        // mcbtodo: This code in this task still needs to be updated to handle the case when it is editing an instance task. Currently it only loads the correct dropdown values for a templated task.

        private void ReloadComboBox_Features()
        {
            ComboBox_Features.Clear();
            var selectedSystemObj = cboSystem.SelectedItem;
            if (selectedSystemObj == null)
            {
                return;
            }
            var selectedSystem = selectedSystemObj as CMSystemDto;
            var cmFeatures = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_ForSystem(selectedSystem.Id, cmTask.IsTemplate);
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
            var cmStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(selectedFeature.Id);
            foreach (var cmState in cmStates)
            {
                ComboBox_States.Add(cmState);
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            TaskData = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(cmTaskId);

            if (TaskData != null)
            {
                try
                {
                    var selectedFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(TaskData.CMFeatureId);

                    cboSystem.SelectedItem = ComboBox_Systems.First(s => s.Id == selectedFeature.CMSystemId);
                    ReloadComboBox_Features();
                    cboFeature.SelectedItem = ComboBox_Features.First(f => f.Id == TaskData.CMFeatureId);
                    ReloadComboBox_States();
                    cboState.SelectedItem = ComboBox_States.First(s => s.Id == TaskData.CMTargetSystemStateId);
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

            if (TaskData == null)
            {
                var taskDataDto = new FeatureDependencyDto()
                {
                    TaskId = cmTaskId,
                    CMFeatureId = selectedFeature.Id,
                    CMTargetSystemStateId = selectedState.Id
                };
                BuildInTasksDataProviders.FeatureDependencyDataProvider.Insert(taskDataDto);
                TaskData = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(cmTaskId);
            }
            else
            {
                TaskData.CMFeatureId = selectedFeature.Id;
                TaskData.CMTargetSystemStateId = selectedState.Id;
                BuildInTasksDataProviders.FeatureDependencyDataProvider.Update(TaskData);
            }
        }
    }
}
