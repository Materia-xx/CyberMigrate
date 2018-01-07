using DataProvider;
using Dto;
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
        private CMSystemDto cmSystem;
        private CMFeatureDto cmFeature;
        private CMTaskDto cmTask;

        private CMFeatureDto cmTargetFeature;

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
            ReloadUI();
        }

        private void ReloadUI()
        {
            TaskData = FeatureDependencyExtensions.FeatureDependencyDataProvider.Get_ForTaskId(cmTask.Id);

            if (TaskData == null)
            {
                TaskData = new FeatureDependencyDto()
                {
                    TaskId = cmTask.Id
                };
            }

            dataGridEditFeatureDependencySettings.Visibility = Visibility.Hidden;
            dataGridChooseFeatureDependency.Visibility = Visibility.Hidden;
            lblChosenFeature.Visibility = Visibility.Hidden;
            lblChosenFeatureName.Visibility = Visibility.Hidden;

            dataGridEditFeatureDependencySettings.ItemsSource = null;
            dataGridChooseFeatureDependency.ItemsSource = null;

            // Display as a task template
            if (cmTask.IsTemplate)
            {
                dataGridEditFeatureDependencySettings.Visibility = Visibility.Visible;

                dataGridEditFeatureDependencySettings.ItemsSource = TaskData.PathOptions;
            }
            // Display as a task instance
            else
            {
                // Display for an instance that does not yet have the choice made
                if (TaskData.InstancedCMFeatureId == 0)
                {
                    dataGridChooseFeatureDependency.Visibility = Visibility.Visible;
                    dataGridChooseFeatureDependency.ItemsSource = TaskData.PathOptions;
                }
                // Display for an instance that already has the choice made
                else
                {
                    lblChosenFeature.Visibility = Visibility.Visible;
                    lblChosenFeatureName.Visibility = Visibility.Visible;

                    cmTargetFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(TaskData.InstancedCMFeatureId);
                    lblChosenFeatureName.Content = cmTargetFeature.Name;
                }
            }
        }

        private void btnSetFeature_Click(object sender, RoutedEventArgs e)
        {
            var rowData = ((FrameworkElement)sender).DataContext as FeatureDependencyPathOptionDto;
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
                UpdateTaskData();

                // This is here to make the button show the correct text on each button after chosing the feature
                // Otherwise it just retains the previous value, even after associating a new feature.
                dataGridEditFeatureDependencySettings.ItemsSource = null;
                dataGridEditFeatureDependencySettings.ItemsSource = TaskData.PathOptions;
            }
        }

        private void btnChooseFeature_Click(object sender, RoutedEventArgs e)
        {
            var rowData = ((FrameworkElement)sender).DataContext as FeatureDependencyPathOptionDto;
            if (rowData == null)
            {
                // Shouldn't happen, but if it does, just ignore it.
                return;
            }

            // Set the chosen feature var. CUD events should take care of the rest of the instancing aspects.
            var newFeatureVar = new CMFeatureVarStringDto()
            {
                CMFeatureId = cmFeature.Id,
                Name = rowData.FeatureVarName,
                Value = rowData.FeatureVarSetTo
            };
            var opResult = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.Insert(newFeatureVar);
            if (opResult.Errors.Any())
            {
                MessageBox.Show(opResult.ErrorsCombined);
                return;
            }

            // Reload the UI so it shows in the chosen mode
            ReloadUI();
        }

        /// <summary>
        /// Updates the task data with whatever is currently set in the TaskData
        /// </summary>
        private void UpdateTaskData()
        {
            if (TaskData.Id == 0)
            {
                FeatureDependencyExtensions.FeatureDependencyDataProvider.Insert(TaskData);
                // Re-get so the db id is assigned
                TaskData = FeatureDependencyExtensions.FeatureDependencyDataProvider.Get_ForTaskId(cmTask.Id);
            }
            else
            {
                FeatureDependencyExtensions.FeatureDependencyDataProvider.Update(TaskData);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UpdateTaskData();
        }
    }
}
