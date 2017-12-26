using DataProvider;
using Dto;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TaskBase;

namespace CyberMigrate.Configuration
{
    /// <summary>
    /// Interaction logic for TaskTypeConfigUC.xaml
    /// </summary>
    public partial class TaskTypeConfigUC : UserControl
    {
        public Config ConfigWindow { get; set; }

        public CMTaskTypeDto cmTaskType;

        private ObservableCollection<CMTaskStateDto> taskStates { get; set; } = new ObservableCollection<CMTaskStateDto>();

        public TaskTypeConfigUC(Config configWindow, CMTaskTypeDto cmTaskType)
        {
            InitializeComponent();
            this.cmTaskType = cmTaskType;
            this.ConfigWindow = configWindow;
        }

        public TaskTypeConfigUC()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Init_TaskStatesGrid();

            var taskTypeUc = TaskFactoriesCatalog.Instance.GetTaskConfigUI(cmTaskType);
            if (taskTypeUc != null)
            {
                TaskTypePluginUC.Children.Add(taskTypeUc);
            }
        }

        private void Init_TaskStatesGrid()
        {
            // Don't let the grid auto-generate the columns. Because we want to instead have some of them hidden
            dataGridTaskStates.AutoGenerateColumns = false;
            dataGridTaskStates.Columns.Clear();

            dataGridTaskStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMTaskStateDto.Priority),
                    Binding = new Binding(nameof(CMTaskStateDto.Priority))
                });
            dataGridTaskStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Name",
                    Binding = new Binding(nameof(CMTaskStateDto.DisplayName)),
                    Width = 200
                });

            // Load all task type states
            dataGridTaskStates.ItemsSource = taskStates;
            Reload_TaskStates();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridTaskStates.RowEditEnding += DataGridTaskState_RowEditEnding;
        }

        private void Reload_TaskStates()
        {
            taskStates.CollectionChanged -= TaskStates_CollectionChanged;
            taskStates.Clear();
            var cmTaskTypeStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(cmTaskType.Id).ToList();
            foreach (var taskState in cmTaskTypeStates)
            {
                taskStates.Add(taskState);
            }
            taskStates.CollectionChanged += TaskStates_CollectionChanged;
        }

        private void TaskStates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removedState in e.OldItems)
                {
                    var gridTaskState = (CMTaskStateDto)removedState;

                    var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Delete(gridTaskState.Id);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Reload the task states grid to represent that the item wasn't actually deleted
                        Reload_TaskStates();
                        return;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // The order of operations (I believe) is:
                //  * A new row is added to the datagrid
                //  * A new CMTaskStateDto is constructed and added to the observable collection.
                //    Note that at this point this Dto *may* not be in a valid state to be entered into the db and an insert operation will fail.
                // Therefore we do not do the insert attempt at this point. Instead it is handled in the row update code.
                // However we do set defaults for things here that won't be available to set through the grid UI
                foreach (var addedState in e.NewItems)
                {
                    var gridTaskState = (CMTaskStateDto)addedState;
                    gridTaskState.TaskTypeId = cmTaskType.Id;
                }
            }
        }

        private void DataGridTaskState_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Unfortunately, I'm unable to find any reference to the "new values" as a Dto object.
                // I imagine it would be possible to dig through the cells and construct it, but IMO that is even worse than this:
                dataGridTaskStates.RowEditEnding -= DataGridTaskState_RowEditEnding;
                dataGridTaskStates.CommitEdit();
                dataGridTaskStates.Items.Refresh();
                dataGridTaskStates.RowEditEnding += DataGridTaskState_RowEditEnding;

                var gridTaskState = (CMTaskStateDto)dataGridTaskStates.SelectedItem;

                if (!gridTaskState.Reserved)
                {
                    gridTaskState.InternalName = gridTaskState.DisplayName;
                }

                if (gridTaskState.Id > 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Update(gridTaskState);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the rules grid
                        Reload_TaskStates();
                        return;
                    }
                }
                else
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Insert(gridTaskState);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Keep the row around so the user has a chance to correct it.
                        return;
                    }
                }
            }
        }
    }
}
