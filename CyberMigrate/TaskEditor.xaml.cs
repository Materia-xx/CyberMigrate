using DataProvider;
using Dto;
using System.Collections.Generic;
using System.Linq;
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
            var taskUC = TaskFactoriesCatalog.Instance.GetTaskUI(ref_TaskTypeDto, ref_SystemDto, ref_FeatureDto, cmTaskDto);
            taskUI.Children.Add(taskUC);
        }

        private void LoadTaskStatesCombo()
        {
            var taskStates = new List<CMTaskStateDto>();
            if (cmTaskDto.IsTemplate)
            {
                // The only state option for a template is "Template"
                var taskStateTemplate = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Template, cmTaskDto.CMTaskTypeId);
                taskStates.Add(taskStateTemplate);
            }
            else
            {
                var allTaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(cmTaskDto.CMTaskTypeId);
                // "Template" isn't an option for instances
                allTaskStates = allTaskStates.Where(s => s.InternalName.Equals("Template") == false);
                taskStates.AddRange(allTaskStates);
            }

            cboTaskStates.DisplayMemberPath = "DisplayName";
            cboTaskStates.SelectedValuePath = "Id";
            cboTaskStates.SelectedValue = cmTaskDto.CMTaskStateId;
            cboTaskStates.ItemsSource = taskStates;

            cboTaskStates.SelectionChanged += CboTaskStates_SelectionChanged;
        }

        private void CboTaskStates_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Don't allow the state to be set for task templates, here it is hardcoded to "Template"
            if (cmTaskDto.IsTemplate)
            {
                MessageBox.Show("Cannot set the state of a task template.");
                return;
            }
            
            var selectedStateObj = cboTaskStates.SelectedItem;
            if (selectedStateObj == null || !(selectedStateObj is CMTaskStateDto))
            {
                return;
            }

            var selectedState = selectedStateObj as CMTaskStateDto;

            // If the task state changed, then update it
            if (cmTaskDto.CMTaskStateId != selectedState.Id)
            {
                cmTaskDto.CMTaskStateId = selectedState.Id;
                var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Update(cmTaskDto);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                }
            }
        }
    }
}
