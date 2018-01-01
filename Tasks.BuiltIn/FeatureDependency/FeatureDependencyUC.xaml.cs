using Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tasks.BuiltIn.FeatureDependency
{
    /// <summary>
    /// Interaction logic for FeatureDependencyUC.xaml
    /// </summary>
    public partial class FeatureDependencyUC : UserControl
    {
        private CMSystemDto cmSystem;
        private CMFeatureDto cmFeature;
        private CMTaskDto cmTask;

        private FeatureDependencyDto TaskData;

        public FeatureDependencyUC(CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            InitializeComponent();

            this.cmSystem = cmSystem;
            this.cmFeature = cmFeature;
            this.cmTask = cmTask;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TaskData = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(cmTask.Id);

            if (TaskData == null)
            {
                TaskData = new FeatureDependencyDto()
                {
                    TaskId = cmTask.Id
                };
            }

            // mcbtodo: Add a way to see the feature that was instanced, after it is.
            dataGridFeatureDependencySettings.ItemsSource = TaskData.PathOptions;
        }

        private void btnSetFeature_Click(object sender, RoutedEventArgs e)
        {
            var selectedRowIndex = dataGridFeatureDependencySettings.SelectedIndex;

            var rowData = dataGridFeatureDependencySettings.SelectedItem as FeatureDependencyPathOptionDto;
            if (rowData == null)
            {
                MessageBox.Show("The row must first be fully inserted before the associated feature can be set.");
                return;
            }

            Window featureSelector = new Window()
            {
                Title = "Select Feature",
                Width = 800,
                Height = 600
            };

            var featureSelectorUC = new FeatureDependencyChooseFeatureUC(rowData.CMFeatureTemplateId, rowData.CMTargetSystemStateId, cmTask.IsTemplate, featureSelector);
            featureSelectorUC.Margin = new Thickness(5);
            featureSelector.Content = featureSelectorUC;
            featureSelector.ShowDialog();

            if (featureSelectorUC.SelectionConfirmed)
            {
                rowData.CMFeatureTemplateId = featureSelectorUC.SelectedFeatureId;
                rowData.CMTargetSystemStateId = featureSelectorUC.SelectedSystemStateId;
                dataGridFeatureDependencySettings.ItemsSource = null;
                dataGridFeatureDependencySettings.ItemsSource = TaskData.PathOptions;
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            foreach (var taskDataRow in TaskData.PathOptions)
            {
                if (taskDataRow.CMFeatureTemplateId == 0)
                {
                    MessageBox.Show("Each row must be assigned to a valid feature.");
                    return;
                }

                if (taskDataRow.CMTargetSystemStateId == 0)
                {
                    MessageBox.Show("Each row must be assigned to a valid sytem state.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(taskDataRow.FeatureVarName) && !string.IsNullOrWhiteSpace(taskDataRow.FeatureVarSetTo))
                {
                    MessageBox.Show("Cannot set a feature var value to check for without specifying the feature var itself.");
                    return;
                }
            }

            // Make sure the options are listed in order
            // mcbtodo: this would be better in the data provider when that is possible
            TaskData.PathOptions = TaskData.PathOptions.OrderBy(po => po.Order).ToList();

            if (TaskData.Id == 0)
            {
                BuildInTasksDataProviders.FeatureDependencyDataProvider.Insert(TaskData);
                // Re-get so the db id is assigned
                TaskData = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(cmTask.Id);
            }
            else
            {
                BuildInTasksDataProviders.FeatureDependencyDataProvider.Update(TaskData);
            }

            MessageBox.Show("Updated");
        }
    }
}
