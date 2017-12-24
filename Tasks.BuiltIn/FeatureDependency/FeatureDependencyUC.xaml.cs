using DataProvider;
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

        private CMTaskDataCRUD<FeatureDependencyDto> TaskDataProvider;

        private FeatureDependencyDto TaskData;

        public FeatureDependencyUC(int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            InitializeComponent();

            this.cmSystemId = cmSystemId;
            this.cmFeatureId = cmFeatureId;
            this.cmTaskId = cmTaskId;

            TaskDataProvider = CMDataProvider.GetTaskTypeDataProvider<FeatureDependencyDto>();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            TaskData = TaskDataProvider.Get_ForTaskId(cmTaskId);

            if (TaskData == null)
            {
                // mcbtodo: update hardcoded values with values from the task UI after it is built.
                var exampleDto = new FeatureDependencyDto()
                {
                    TaskId = cmTaskId,
                    CMFeatureId = 1,
                    CMTargetSystemStateId = 2
                };
                TaskDataProvider.Insert(exampleDto);
                TaskData = TaskDataProvider.Get_ForTaskId(cmTaskId);
            }

            // mcbtodo: add in the real code for this task, create the real UI for this task also
            // Change the demo to the task name to demonstrate data provider access

            var taskDto = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmTaskId);
            lblDemo.Content = taskDto.Title;
        }
    }
}
