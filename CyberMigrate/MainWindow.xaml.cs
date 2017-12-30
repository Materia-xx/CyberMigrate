using CyberMigrate.Extensions;
using DataProvider;
using DataProvider.Events;
using Dto;
using System;
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

        /// <summary>
        /// Keeps track if each tack factory has been called to register its callback events or not so we
        /// don't end up calling the same factory 2 times and double registering callbacks.
        /// </summary>
        private Dictionary<string, bool> taskFactoryRegisteredCallbacks = new Dictionary<string, bool>();

        /// <summary>
        /// Keeps track if the state calculation callback events have been registered yet.
        /// </summary>
        private bool stateCalculationCallbacksRegistered = false;

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

            dataGridTasks.ItemsSource = filterResults;

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
            var systemsLookup = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll_AsLookup();
            var featureInstancesLookup = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances_AsLookup();
            var systemStatesLookup = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_AsLookup();
            var taskStatesLookup = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_AsLookup();
            var taskTypesLookup = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll_AsLookup();

            // mcbtodo: add a way to query for just task instances that are open
            var filteredTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Instances();

            var attachedTag = treeFilter.GetSelectedTreeViewTag();

            int filterSystemId = 0;
            int filterSystemStateId = 0;
            bool filterOnlyShowTasksInCurrentFeatureState = true;

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

                var featureRef = featureInstancesLookup[cmTask.CMFeatureId];
                var systemRef = systemsLookup[featureRef.CMSystemId];

                // Filter out tasks in system if filter is set
                if (filterSystemId != 0 && systemRef.Id != filterSystemId)
                {
                    continue;
                }

                var featureSystemStateRef = systemStatesLookup[featureRef.CMSystemStateId];
                bool taskIsInCurrentFeatureState = featureSystemStateRef.Id == cmTask.CMSystemStateId;
                // Filter out tasks that are not in the current feature state
                if (filterOnlyShowTasksInCurrentFeatureState && !taskIsInCurrentFeatureState)
                {
                    continue;
                }

                var taskSystemStateRef = systemStatesLookup[cmTask.CMSystemStateId];
                var taskStateRef = taskStatesLookup[cmTask.CMTaskStateId];
                var taskTypeRef = taskTypesLookup[cmTask.CMTaskTypeId];

                var filterRow = new FilterResultItem()
                {
                    SystemStatePriorityId = taskSystemStateRef.Priority,
                    FeatureName = featureRef.Name,
                    TaskTitle = cmTask.Title,
                    TaskId = cmTask.Id,
                    TaskType = taskTypeRef.Name,
                    TaskStatePriorityId = taskStateRef.Priority,
                    TaskStateName = taskStateRef.DisplayName,
                    SystemName = systemRef.Name,
                    TaskSystemStateName = taskSystemStateRef.Name,
                    FeatureSystemStateName = featureSystemStateRef.Name,
                };

                unsortedResults.Add(filterRow);
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
            var registerError = RegisterTaskFactories_InDatabase();
            if (!string.IsNullOrWhiteSpace(registerError))
            {
                // If there is any errors with registering the task types then close the main window
                // Assuming that this means the program will not be able to run successfully without the property tasks registered
                MessageBox.Show(registerError);
                this.Close();
                return;
            }
            RegisterTaskFactory_Callbacks();
            RegisterStateCalculation_Callbacks();
        }

        private void RegisterStateCalculation_Callbacks()
        {
            if (stateCalculationCallbacksRegistered)
            {
                return;
            }
            stateCalculationCallbacksRegistered = true;

            // All things that should result in refreshing the cached lookup tables used for state calculation 
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_Created_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_Updated_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordDeleted += Record_Deleted_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordCreated += Record_Created_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_Updated_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordDeleted += Record_Deleted_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordCreated += Record_Created_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordUpdated += Record_Updated_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordDeleted += Record_Deleted_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordCreated += Record_Created_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordUpdated += Record_Updated_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordDeleted += Record_Deleted_StateCalcLookupsRefreshNeeded;

            // All things that should result in the re-calculation of *all* current feature system states
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordCreated += Record_Created_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordUpdated += Record_Updated_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordDeleted += Record_Deleted_CalculateAllFeatureStates;

            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_Created_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_Updated_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordDeleted += Record_Deleted_CalculateAllFeatureStates;

            // Feature Create: Updating the feature state for a new feature is handled during the creation of the feature
            // Feature Delete: If a feature is deleted and there is a dependency task pointing at it, it may cause the dependency task to calculate its task state differently
            //                 However the StateCalculations.CalculateAllFeatureStates() call to calc all feature states does not call in to calculate task states first so we ignore deletes here.
            // Feature Update: If the code is right this shouldn't cause a stack overflow if the depth of features isn't too deep.
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_Updated_CalculateAllFeatureStates;
        }

        /// <summary>
        /// Call by the CRUD providers when the lookups used by the state calculation routines need to be refreshed
        /// </summary>
        private void Record_Created_StateCalcLookupsRefreshNeeded(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs)
        {
            StateCalculations.LookupsRefreshNeeded = true;
        }

        /// <summary>
        /// Call by the CRUD providers when the lookups used by the state calculation routines need to be refreshed
        /// </summary>
        private void Record_Updated_StateCalcLookupsRefreshNeeded(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            StateCalculations.LookupsRefreshNeeded = true;
        }

        /// <summary>
        /// Call by the CRUD providers when the lookups used by the state calculation routines need to be refreshed
        /// </summary>
        private void Record_Deleted_StateCalcLookupsRefreshNeeded(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs)
        {
            StateCalculations.LookupsRefreshNeeded = true;
        }

        private void Record_Created_CalculateAllFeatureStates(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs)
        {
            StateCalculations.CalculateAllFeatureStates();
        }

        private void Record_Updated_CalculateAllFeatureStates(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            StateCalculations.CalculateAllFeatureStates();
        }

        private void Record_Deleted_CalculateAllFeatureStates(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs)
        {
            StateCalculations.CalculateAllFeatureStates();
        }

        private void RegisterTaskFactory_Callbacks()
        {
            var taskFactories = TaskFactoriesCatalog.Instance.TaskFactories.ToList();

            foreach (var taskFactory in taskFactories)
            {
                if (taskFactoryRegisteredCallbacks.ContainsKey(taskFactory.Name))
                {
                    continue;
                }

                taskFactoryRegisteredCallbacks[taskFactory.Name] = true;
                taskFactory.RegisterCMCUDCallbacks();
            }
        }

        /// <summary>
        /// Taskes care of scanning for task factories and registering them in the database if needed.
        /// </summary>
        /// <returns></returns>
        private string RegisterTaskFactories_InDatabase()
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
                    var reservedInternalTaskStates = ReservedTaskStates.States;
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

        private void btnViewTask_Click(object sender, RoutedEventArgs e)
        {
            var selectedRowIndex = dataGridTasks.SelectedIndex;

            if (filterResults.Count() <= selectedRowIndex)
            {
                // This means clicking on the button of a new row, but this shouldn't be possible so just ignore it.
                return;
            }

            var cmFilterData = filterResults[selectedRowIndex];
            var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmFilterData.TaskId);

            var taskEditor = new TaskEditor(cmTask);

            // mcbtodo: keep track of open windows and just switch the already open one if it is, otherwise we open it here.
            taskEditor.Show();
        }
    }
}
