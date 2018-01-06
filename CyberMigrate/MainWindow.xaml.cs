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
        /// Keeps track if each tack factory has been called to be initialized so we
        /// don't end up calling the same factory 2 times.
        /// </summary>
        private Dictionary<string, bool> taskFactoryInitialized = new Dictionary<string, bool>();

        private class AllSystemsDto : IdBasedObject { }

        /// <summary>
        /// Keeps track if the internal CUD callbacks have been registered yet.
        /// </summary>
        private bool internalCUDCallbacksRegistered = false;

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
            var attachedTag = treeFilter.GetSelectedTreeViewTag();
            TreeViewItem plannedSelectionTVI = null;

            treeFilter.Items.Clear();

            var allSystemsTVI = GetTVI_AllSystems();
            treeFilter.Items.Add(allSystemsTVI);

            // Get all of the systems and show them as tree view items
            var cmSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();

            foreach (var cmSystem in cmSystems)
            {
                var cmSystemTVI = GetTVI_System(cmSystem);
                allSystemsTVI.Items.Add(cmSystemTVI);
                SetPlannedTVIToSelect_IfMatch(ref plannedSelectionTVI, attachedTag?.Dto, cmSystemTVI, cmSystem);

                // Show all system states available in this system
                var cmSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystem.Id);

                foreach(var cmSystemState in cmSystemStates)
                {
                    var cmSystemStateTVI = GetTVI_SystemState(cmSystemState);
                    cmSystemTVI.Items.Add(cmSystemStateTVI);
                    SetPlannedTVIToSelect_IfMatch(ref plannedSelectionTVI, attachedTag?.Dto, cmSystemStateTVI, cmSystemState);

                    // Get all features that are currently in this state and show them
                    var cmFeatures = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances_ForSystemState(cmSystemState.Id);

                    foreach (var cmFeature in cmFeatures)
                    {
                        var cmFeatureTVI = GetTVI_Feature(cmFeature);
                        cmSystemStateTVI.Items.Add(cmFeatureTVI);
                        SetPlannedTVIToSelect_IfMatch(ref plannedSelectionTVI, attachedTag?.Dto, cmFeatureTVI, cmFeature);
                    }
                }

                cmSystemTVI.ExpandSubtree();
            }

            // If no node was found to set as the selected one, then default to the "All Systems" node
            if (plannedSelectionTVI == null)
            {
                plannedSelectionTVI = allSystemsTVI;
            }

            // Select the node that is supposed to be selected so that it lists the tasks
            plannedSelectionTVI.IsSelected = true;
        }

        private void SetPlannedTVIToSelect_IfMatch(ref TreeViewItem plannedSelectionTVI, IdBasedObject previouslySelectedDto, TreeViewItem prospectTVI, IdBasedObject prospectDto)
        {
            // If there is already a TVI planned to be selected
            if (plannedSelectionTVI != null)
            {
                return;
            }

            // If either of the Dtos are null then don't choose this node, the code will manually
            // select the "All Systems" node above if this ends up being the case for everything
            if (previouslySelectedDto == null || prospectDto == null)
            {
                return;
            }

            var previousType = previouslySelectedDto.GetType().Name;
            var prospectType = prospectDto.GetType().Name;

            // If the previous Dto is the not same type as the prospect, then it is not a match
            if (previousType.Equals(prospectType) == false)
            {
                return;
            }

            // We know the Dto types match, now just compare the ids to see if it is the same record
            if (previouslySelectedDto.Id == prospectDto.Id)
            {
                plannedSelectionTVI = prospectTVI;
            }
        }

        private void TreeFilter_NodeSelected(object sender, RoutedEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void ShowFilteredTasks()
        {
            filterResults.Clear();

            var attachedTag = treeFilter.GetSelectedTreeViewTag();

            int filterSystemId = 0;
            int filterSystemStateId = 0;
            int filterFeatureId = 0;
            bool filterOnlyShowTasksInCurrentFeatureState = true;

            if (attachedTag?.Dto == null)
            {
                // Unknown filter node, list nothing
                return;
            }
            else
            {
                switch (attachedTag.DtoTypeName)
                {
                    case nameof(AllSystemsDto):
                        // No filters, list everything
                        break;
                    case nameof(CMSystemDto):
                        var filterSystem = attachedTag.Dto as CMSystemDto;
                        filterSystemId = filterSystem.Id;
                        break;
                    case nameof(CMSystemStateDto):
                        var filterSystemState = attachedTag.Dto as CMSystemStateDto;
                        filterSystemStateId = filterSystemState.Id;
                        break;
                    case nameof(CMFeatureDto):
                        var filterFeature = attachedTag.Dto as CMFeatureDto;
                        filterFeatureId = filterFeature.Id;
                        filterOnlyShowTasksInCurrentFeatureState = false;
                        break;
                    default:
                        // Also unknown filter node, list nothing
                        return;
                }
            }

            // Default is to list tasks in all systems/features/states
            var systemsLookup = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll_AsLookup();
            var featureInstancesLookup = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances_AsLookup();
            var systemStatesLookup = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_AsLookup();
            var taskStatesLookup = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_AsLookup();
            var taskTypesLookup = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll_AsLookup();

            // mcbtodo: add a way to query for just task instances that are open
            var filteredTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Instances();

            // Show the results
            var unsortedResults = new List<FilterResultItem>();
            foreach (var cmTask in filteredTasks)
            {
                // Filter to just tasks in the specified system state (if filter is set)
                if (filterSystemStateId != 0 && cmTask.CMSystemStateId != filterSystemStateId)
                {
                    continue;
                }

                var featureRef = featureInstancesLookup[cmTask.CMFeatureId];

                // Filter to just tasks in the specified feature (if filter is set)
                if (filterFeatureId !=0 && featureRef.Id != filterFeatureId)
                {
                    continue;
                }

                var systemRef = systemsLookup[featureRef.CMSystemId];

                // Filter to just tasks in the specified system (if filter is set)
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

        private TreeViewItem GetTVI_AllSystems()
        {
            var allSystemsTVI = new TreeViewItem()
            {
                Header = "All Systems",
                Tag = new TreeViewTag(new AllSystemsDto())
            };

            // Add the context menu
            // There is no context menu actions currently for task factories
            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            allSystemsTVI.ContextMenu = contextMenu;

            allSystemsTVI.Selected += TreeFilter_NodeSelected; // Still keep the onSelected event so the UI can clear what may be there when selected

            return allSystemsTVI;
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
                newFeatureSubMenu.Click += (sender, e) =>
                {
                    var newFeature = cmFeatureTemplate.ToInstance(new List<CMFeatureVarStringDto>());
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

        private TreeViewItem GetTVI_Feature(CMFeatureDto cmFeature)
        {
            var cmFeatureTVI = new TreeViewItem()
            {
                Header = cmFeature.Name,
                Tag = new TreeViewTag(cmFeature)
            };

            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            cmFeatureTVI.ContextMenu = contextMenu;

            return cmFeatureTVI;
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
            TaskFactories_Init();
            RegisterInternalCUDCallbacks();
        }

        private void RegisterInternalCUDCallbacks()
        {
            if (internalCUDCallbacksRegistered)
            {
                return;
            }
            internalCUDCallbacksRegistered = true;

            // All things that should result in refreshing the cached lookup tables used for state calculation 
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            // All things that should result in the re-calculation of *all* current feature system states
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordCreated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordUpdated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordDeleted += Record_CUD_CalculateAllFeatureStates;

            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordDeleted += Record_CUD_CalculateAllFeatureStates;

            // Feature Create: Updating the feature state for a new feature is handled during the creation of the feature
            // Feature Delete: If a feature is deleted and there is a dependency task pointing at it, it may cause the dependency task to calculate its task state differently
            //                 However the StateCalculations.CalculateAllFeatureStates() call to calc all feature states does not call in to calculate task states first so we ignore deletes here.
            // Feature Update: If the code is right this shouldn't cause a stack overflow if the depth of features isn't too deep.
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_CalculateAllFeatureStates;

            // React to when new feature vars are added. A title of a feature may be waiting for this or a task may be waiting to make
            // a decision based on a feature var.
            CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.OnRecordCreated += FeatureVar_Created;

            // Refresh the filter tree view when needed
            CMDataProvider.DataStore.Value.CMSystems.Value.OnRecordCreated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMSystems.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMSystems.Value.OnRecordDeleted += Record_CUD_FilterTreeViewRefreshNeeded;

            CMDataProvider.DataStore.Value.CMSystemStates.Value.OnRecordCreated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMSystemStates.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMSystemStates.Value.OnRecordDeleted += Record_CUD_FilterTreeViewRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordCreated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordDeleted += Record_CUD_FilterTreeViewRefreshNeeded;

            // Refresh the list of tasks when needed
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;

            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_CUD_RefreshFilteredTasks;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_CUD_RefreshFilteredTasks;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordDeleted += Record_CUD_RefreshFilteredTasks;
        }

        private void Record_CUD_RefreshFilteredTasks(object recordEventArgs)
        {
            ShowFilteredTasks();
        }

        /// <summary>
        /// Redraws the filter tree view when associated records are created, updated or deleted
        /// </summary>
        /// <param name="recordEventArgs"></param>
        private void Record_CUD_FilterTreeViewRefreshNeeded(object recordEventArgs)
        {
            RedrawFilterTreeView();
        }

        /// <summary>
        /// Called by the CRUD providers when the lookups used by the state calculation routines need to be refreshed
        /// </summary>
        private void Record_CUD_StateCalcLookupsRefreshNeeded(object recordEventArgs)
        {
            StateCalculations.LookupsRefreshNeeded = true;
        }

        /// <summary>
        /// Called by the CRUD providers when the feature states need to be re-calculated
        /// </summary>
        /// <param name="createdRecordEventArgs"></param>
        private void Record_CUD_CalculateAllFeatureStates(object recordEventArgs)
        {
            StateCalculations.CalculateAllFeatureStates();
        }

        /// <summary>
        /// Called by the CRUD providers when a new feature var is inserted and things that reference that var need to be resolved
        /// </summary>
        /// <param name="createdRecordEventArgs"></param>
        private void FeatureVar_Created(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs)
        {
            var featureVar = createdRecordEventArgs.CreatedDto as CMFeatureVarStringDto;
            var feature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(featureVar.CMFeatureId);
            feature.ResolveFeatureVarsForFeatureAndTasks();
        }

        private void TaskFactories_Init()
        {
            var taskFactories = TaskFactoriesCatalog.Instance.TaskFactories.ToList();

            foreach (var taskFactory in taskFactories)
            {
                if (taskFactoryInitialized.ContainsKey(taskFactory.Name))
                {
                    continue;
                }

                taskFactoryInitialized[taskFactory.Name] = true;
                taskFactory.Initialize();
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

        private void btnViewFeature_Click(object sender, RoutedEventArgs e)
        {
            var selectedRowIndex = dataGridTasks.SelectedIndex;

            if (filterResults.Count() <= selectedRowIndex)
            {
                // This means clicking on the button of a new row, but this shouldn't be possible so just ignore it.
                return;
            }

            var cmFilterData = filterResults[selectedRowIndex];
            var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmFilterData.TaskId);
            var cmFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmTask.CMFeatureId);

            var featureEditor = new FeatureEditor(cmFeature);

            featureEditor.ShowDialog();
        }
    }
}
