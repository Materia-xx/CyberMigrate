using DataProvider;
using Dto;
using System.Collections.Generic;
using System.Windows;
using TaskBase;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for Task.xaml
    /// </summary>
    public partial class TaskEditor : Window
    {
        private CMTaskDto cmTaskDto;

        private CMTaskTypeDto ref_TaskTypeDto;
        private CMSystemDto ref_SystemDto;
        private CMFeatureDto ref_FeatureDto;

        public TaskEditor(CMTaskDto cmTaskDto)
        {
            this.cmTaskDto = cmTaskDto;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = cmTaskDto.Title;

            ref_TaskTypeDto = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(cmTaskDto.CMTaskTypeId);
            ref_FeatureDto = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmTaskDto.CMFeatureId);
            ref_SystemDto = CMDataProvider.DataStore.Value.CMSystems.Value.Get(ref_FeatureDto.CMSystemId);

            LoadTaskStatesCombo();
            LoadTaskUI();
        }

        private void LoadTaskUI()
        {
            var taskUC = TaskFactoriesCatalog.Instance.GetTaskUI(ref_TaskTypeDto.Name, ref_SystemDto.Id, ref_FeatureDto.Id, cmTaskDto.Id);
            taskUI.Children.Add(taskUC);
        }

        private void LoadTaskStatesCombo()
        {
            var taskStates = new List<CMTaskStateDto>();
            if (cmTaskDto.IsTemplate)
            {
                var taskStateTemplate = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(CMTaskStatesCRUD.InternalState_Template, cmTaskDto.CMTaskTypeId);
                taskStates.Add(taskStateTemplate);
            }
            else
            {
                var allTaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(cmTaskDto.CMTaskTypeId);
                taskStates.AddRange(allTaskStates);
            }

            cboTaskStates.DisplayMemberPath = "DisplayName";
            cboTaskStates.SelectedValuePath = "Id";
            cboTaskStates.SelectedValue = cmTaskDto.CMTaskStateId;
            cboTaskStates.ItemsSource = taskStates;
        }

    }
}
