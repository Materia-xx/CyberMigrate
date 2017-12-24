using DataProvider;
using System.Windows.Controls;

namespace Tasks.BuiltIn
{
    /// <summary>
    /// Interaction logic for FeatureDependencyUC.xaml
    /// </summary>
    public partial class FeatureDependencyUC : UserControl
    {
        private int cmSystemId;
        private int cmFeatureId;
        private int cmTaskId;

        public FeatureDependencyUC(int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            this.cmSystemId = cmSystemId;
            this.cmFeatureId = cmFeatureId;
            this.cmTaskId = cmTaskId;

            InitializeComponent();
        }

        private void Grid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // mcbtodo: add in the real code for this task 
            // Change the demo to the task name to demonstrate data provider access

            // mcbtodo: full access to the db intended here, or the encapsulated task data ?
            var taskDto = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmTaskId);
            lblDemo.Content = taskDto.Title;
        }
    }
}
