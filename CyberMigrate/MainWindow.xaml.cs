using CyberMigrate.Extensions;
using DataProvider;
using Dto;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TaskBase;
using TaskBase.Extensions;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<FilterResultItem> filterResults = new ObservableCollection<FilterResultItem>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Automatically show the configuration if the program has not been set up.
            if (!DataStoreOptionConfigured())
            {
                ShowConfigurationUI();
                RedrawMainMenu();
                return;
            }

            // Everything past this point depends on the data store being set up already
            DataStorePathSet();

            // Debugging function to clean out instanced tasks and features
            DBMaintenance.RunMaintenanceRoutines();

            Init_TasksGrid();

            // mcbtodo: Move call to correct places
            //StateCalculations.CalculateAllFeatureStates();

            // Select the node that is hovered over when right clicking and before showing the context menu
            treeFilter.PreviewMouseRightButtonDown += TreeViewExtensions.TreeView_PreviewMouseRightButtonDown_SelectNode;
            RedrawFilterTreeView();
            RedrawMainMenu();
        }

        private void RedrawFilterTreeView()
        {
            treeFilter.Items.Clear();

            var allSystemsTVI = GetTVI_AllSystems();
            treeFilter.Items.Add(allSystemsTVI);

            // Get all of the systems and show them as tree view items
            var cmSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();

            foreach (var cmSystem in cmSystems)
            {
                var cmSystemTVI = GetTVI_System(cmSystem);
                allSystemsTVI.Items.Add(cmSystemTVI);

                // Show all system states available in this system
                var cmSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystem.Id);

                foreach(var cmSystemState in cmSystemStates)
                {
                    var cmSystemStateTVI = GetTVI_SystemState(cmSystemState);
                    cmSystemTVI.Items.Add(cmSystemStateTVI);
                }

                cmSystemTVI.ExpandSubtree();
            }

            // Show all tasks by default in the filter grid by selecting the 'All Systems' node.
            allSystemsTVI.IsSelected = true;
        }

        private void Init_TasksGrid()
        {
            dataGridTasks.AutoGenerateColumns = false;
            dataGridTasks.CanUserAddRows = false;
            dataGridTasks.Columns.Clear();

            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "System",
                    Width = 150,
                    Binding = new Binding(nameof(FilterResultItem.SystemName)),
                });

            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Feature",
                    Width = 150,
                    Binding = new Binding(nameof(FilterResultItem.FeatureName)),
                });

            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Feature System State",
                    Width = 150,
                    Binding = new Binding(nameof(FilterResultItem.FeatureSystemStateName)),
                });

            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Task Title",
                    Width = 250,
                    Binding = new Binding(nameof(FilterResultItem.TaskTitle)),
                });

            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Task System State",
                    Width = 150,
                    Binding = new Binding(nameof(FilterResultItem.TaskSystemStateName)),
                });

            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Task State",
                    Width = 150,
                    Binding = new Binding(nameof(FilterResultItem.TaskStateName)),
                });

            // A factory because each row will generate a button
            var viewButtonFactory = new FrameworkElementFactory(typeof(Button));
            viewButtonFactory.SetValue(Button.ContentProperty, "View");
            // mcbtodo: viewButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(btnEditTask_Click));
            dataGridTasks.Columns.Add(new DataGridTemplateColumn
            {
                Header = "View Task",
                CellTemplate = new DataTemplate()
                {
                    VisualTree = viewButtonFactory
                }
            });

            dataGridTasks.ItemsSource = filterResults;

            // Note: This is a set of view-only filtered results, no edits allowed here.
        }

        private TreeViewItem GetTVI_AllSystems()
        {
            var allSystemsTVI = new TreeViewItem()
            {
                Header = "All Systems",
                Tag = null, // There is no tag here because there is no need to show any UI at this level.
            };

            // Add the context menu
            // There is no context menu actions currently for task factories
            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            allSystemsTVI.ContextMenu = contextMenu;

            allSystemsTVI.Selected += TreeFilter_NodeSelected; // Still keep the onSelected event so the UI can clear what may be there when selected

            return allSystemsTVI;
        }

        private void TreeFilter_NodeSelected(object sender, RoutedEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void ShowFilteredTasks()
        {
            filterResults.Clear();

            // Default is to list tasks in all systems/features/states
            // mcbtodo: Provide a different GetAll_AsLookupById that gives back a dictionary<int, CMSystem> and re-write the .first() logic below so it doesn't need to scan through the entire results on each row creation
            var systemsLookup = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();
            var featureInstancesLookup = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances();
            var systemStatesLookup = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll();
            var taskStatesLookup = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll();

            // mcbtodo: add a way to query for just task instances that are open
            var filteredTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Instances();

            var attachedTag = treeFilter.GetSelectedTreeViewTag();

            int filterSystemId = 0;
            int filterSystemStateId = 0;

            if (attachedTag?.Dto == null)
            {
                // list everything
            }
            else
            {
                switch (attachedTag.DtoTypeName)
                {
                    case nameof(CMSystemDto):
                        var filterSystem = attachedTag.Dto as CMSystemDto;
                        filterSystemId = filterSystem.Id;
                        break;
                    case nameof(CMSystemStateDto):
                        var filterSystemState = attachedTag.Dto as CMSystemStateDto;
                        filterSystemStateId = filterSystemState.Id;
                        break;
                }
            }

            // Show the results
            var unsortedResults = new List<FilterResultItem>();
            foreach (var cmTask in filteredTasks)
            {
                // Filter out tasks in system state if filter is set
                if (filterSystemStateId != 0 && cmTask.CMSystemStateId != filterSystemStateId)
                {
                    continue;
                }

                var featureRef = featureInstancesLookup.First(f => f.Id == cmTask.CMFeatureId);
                var systemRef = systemsLookup.First(s => s.Id == featureRef.CMSystemId);

                // Filter out tasks in system if filter is set
                if (filterSystemId != 0 && systemRef.Id != filterSystemId)
                {
                    continue;
                }

                var taskSystemStateRef = systemStatesLookup.First(s => s.Id == cmTask.CMSystemStateId);
                var taskStateRef = taskStatesLookup.First(ts => ts.Id == cmTask.CMTaskStateId);
                var featureSystemStateRef = systemStatesLookup.First(s => s.Id == featureRef.CMSystemStateId);

                unsortedResults.Add(new FilterResultItem()
                {
                    SystemName = systemRef.Name,
                    TaskSystemStateName = taskSystemStateRef.Name,
                    SystemStatePriorityId = taskSystemStateRef.Priority,
                    FeatureName = featureRef.Name,
                    FeatureSystemStateName = featureSystemStateRef.Name,
                    TaskTitle = cmTask.Title,
                    TaskId = cmTask.Id,
                    TaskStatePriorityId = taskStateRef.Priority,
                    TaskStateName = taskStateRef.DisplayName
                });
            }

            // Order the results
            var sortedResults = unsortedResults
                .OrderBy(r => r.SystemStatePriorityId)
                .ThenBy(r => r.TaskStatePriorityId);

            // Add the sorted results to the collection that the grid is watching
            foreach (var sortedResult in sortedResults)
            {
                filterResults.Add(sortedResult);
            }
        }

        private TreeViewItem GetTVI_System(CMSystemDto cmSystem)
        {
            var cmSystemTreeViewItem = new TreeViewItem()
            {
                Header = cmSystem.Name,
                Tag = new TreeViewTag(cmSystem)
            };

            var contextMenu = new ContextMenu();

            var addNewFeature = new MenuItem()
            {
                Header = "New Feature"
            };
            contextMenu.Items.Add(addNewFeature);

            var cmFeatureTemplates = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_ForSystem(cmSystem.Id, true);
            foreach (var cmFeatureTemplate in cmFeatureTemplates)
            {
                var newFeatureSubMenu = new MenuItem()
                {
                    Header = cmFeatureTemplate.Name
                };
                addNewFeature.Items.Add(newFeatureSubMenu);
                addNewFeature.Click += (sender, e) =>
                {
                    var newFeature = cmFeatureTemplate.CreateFeatureInstance(0);
                    ShowFilteredTasks();
                };
            }

            cmSystemTreeViewItem.ContextMenu = contextMenu;

            return cmSystemTreeViewItem;
        }

        private TreeViewItem GetTVI_SystemState(CMSystemStateDto cmSystemState)
        {
            var cmSystemStateTVI = new TreeViewItem()
            {
                Header = cmSystemState.Name,
                Tag = new TreeViewTag(cmSystemState)
            };

            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            cmSystemStateTVI.ContextMenu = contextMenu;

            return cmSystemStateTVI;
        }

        /// <summary>
        /// Call this after the data store path has been set, or verified that it is set after startup.
        /// It takes care of registering everything for the new or pre-existing data store
        /// </summary>
        public void DataStorePathSet()
        {
            var registerError = RegisterTaskFactories();
            if (!string.IsNullOrWhiteSpace(registerError))
            {
                // If there is any errors with registering the task types then close the main window
                // Assuming that this means the program will not be able to run successfully without the property tasks registered
                MessageBox.Show(registerError);
                this.Close();
                return;
            }
        }

        /// <summary>
        /// Taskes care of scanning for task factories and registering them in the database if needed.
        /// </summary>
        /// <returns></returns>
        private string RegisterTaskFactories()
        {
            var taskFactories = TaskFactoriesCatalog.Instance.TaskFactories.ToList();

            var currentFactoriesFromDisk = new List<string>();
            var currentTaskTypesFromDisk = new List<string>();

            // Check to make sure each task factory is available by its name
            foreach (var taskFactory in taskFactories)
            {
                var cmTaskFactory = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Get_ForName(taskFactory.Name);
                if (cmTaskFactory == null)
                {
                    var newTaskFactoryDto = new CMTaskFactoryDto()
                    {
                        Name = taskFactory.Name
                    };

                    var opResult = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Insert(newTaskFactoryDto);
                    if (opResult.Errors.Any())
                    {
                        return opResult.ErrorsCombined;
                    }
                    cmTaskFactory = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Get_ForName(taskFactory.Name);
                }
                if (currentFactoriesFromDisk.Contains(cmTaskFactory.Name))
                {
                    return $"There is more than 1 task factory registering with the same name {cmTaskFactory.Name}. Please resolve this before running the program.";
                }
                currentFactoriesFromDisk.Add(cmTaskFactory.Name);

                // Make sure all of the task types that this task factory provides are registered in the database
                foreach (var taskTypeName in taskFactory.GetTaskTypes())
                {
                    var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(taskTypeName);
                    if (cmTaskType == null)
                    {
                        var newTaskTypeDto = new CMTaskTypeDto()
                        {
                            Name = taskTypeName
                        };

                        var opResult = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Insert(newTaskTypeDto);
                        if (opResult.Errors.Any())
                        {
                            return opResult.ErrorsCombined;
                        }
                        cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(taskTypeName);
                    }
                    if (currentTaskTypesFromDisk.Contains(cmTaskType.Name))
                    {
                        return $"There is more than 1 task type registering with the same name {cmTaskType.Name}. Please resolve this before running the program.";
                    }
                    currentTaskTypesFromDisk.Add(cmTaskType.Name);

                    // Make sure the task states for this task type are registered
                    // First make sure the built in states are present
                    var reservedInternalTaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.InternalStates;
                    var reservedTaskPluginStates = taskFactory.GetRequiredTaskStates(cmTaskType);
                    var invalidPluginStates = reservedTaskPluginStates.Intersect(reservedInternalTaskStates);
                    if (invalidPluginStates.Any())
                    {
                        var allInvalidStates = string.Join(",", invalidPluginStates);
                        return $"The task factory {taskFactory.Name} is attempting to use reserved state(s) {allInvalidStates}. Please remove this task factory and try again.";
                    }
                    var allReservedTaskStates = reservedInternalTaskStates.Union(reservedTaskPluginStates);
                    int priority = 0;
                    foreach (var taskState in allReservedTaskStates)
                    {
                        var dbTaskState = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(taskState, cmTaskType.Id);
                        if (dbTaskState == null)
                        {
                            var newTaskStateDto = new CMTaskStateDto()
                            {
                                DisplayName = taskState,
                                InternalName = taskState,
                                Reserved = true,
                                TaskTypeId = cmTaskType.Id,
                                Priority = ++priority
                            };
                            var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Insert(newTaskStateDto);
                            if (opResult.Errors.Any())
                            {
                                return opResult.ErrorsCombined;
                            }
                        }
                        else
                        {
                            dbTaskState.InternalName = taskState;
                            dbTaskState.DisplayName = taskState;
                            dbTaskState.Reserved = true;
                            var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Update(dbTaskState);
                            if (opResult.Errors.Any())
                            {
                                return opResult.ErrorsCombined;
                            }
                        }
                    }

                    // Un-reserve states that are not required to be reserved now, just in case we are upgrading the db
                    var dbTaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(cmTaskType.Id);
                    foreach (var dbTaskState in dbTaskStates)
                    {
                        if (dbTaskState.Reserved && !allReservedTaskStates.Contains(dbTaskState.InternalName))
                        {
                            dbTaskState.Reserved = false;
                            var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Update(dbTaskState);
                            if (opResult.Errors.Any())
                            {
                                return opResult.ErrorsCombined;
                            }
                        }
                    }
                }
            }

            // Go through everything that is currently registered in the db and check for things that are now missing on disk
            // mcbtodo: for now I'm just showing an error here, but there should be a way to either automatically resolve this
            // mcbtodo: issue or give instructions to the user on how to clean up any references to the removed taskfactory/tasktype.


            // mcbtodo: nothing referenced the factories yet, add this back in or delete as some point
            //var dbTaskFactories = CMDataProvider.DataStore.Value.CMTaskFactories.Value.GetAll();
            //foreach (var dbTaskFactory in dbTaskFactories)
            //{
            //    if (!currentFactoriesFromDisk.Contains(dbTaskFactory.Name))
            //    {
            //        return $"Task factory with name {dbTaskFactory.Name} that was previously registered has been removed. Please put this task factory back in place so the program can run properly.";
            //    }
            //}
            var dbTaskTypes = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll();
            foreach (var dbTaskType in dbTaskTypes)
            {
                // The only restriction is that task types are named uniquely across the collection. It is acceptable if a task type moves from one factory to another.
                if (!currentTaskTypesFromDisk.Contains(dbTaskType.Name))
                {
                    return $"Task type {dbTaskType.Name} that was previously registered has been removed. Please restore the previous configuration so the program can run properly.";
                }
            }

            return null;
        }

        private bool DataStoreOptionConfigured()
        {
            var options = CMDataProvider.Master.Value.GetOptions();
            if (!Directory.Exists(options.DataStorePath))
            {
                return false;
            }
            return true;
        }

        public void ShowConfigurationUI()
        {
            var optionsWindow = new Config(this);
            optionsWindow.ShowDialog();
        }

        public void RedrawMainMenu()
        {
            MainMenu.Items.Clear();

            var configurationMenu = new MenuItem() { Header = "Config" };
            configurationMenu.Click += (sender, e) =>
            {
                ShowConfigurationUI();
            };
            MainMenu.Items.Add(configurationMenu);
        }
    }
}
