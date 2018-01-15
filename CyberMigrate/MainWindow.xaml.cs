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

        private ObservableCollection<CheckBoxListItem<string>> filterTaskStates = new ObservableCollection<CheckBoxListItem<string>>();

        private ObservableCollection<CheckBoxListItem<string>> filterFeatureStates = new ObservableCollection<CheckBoxListItem<string>>();

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

            dataGridTasks.ItemsSource = filterResults;
            lstFilterByTaskState.ItemsSource = filterTaskStates;
            lstFilterByFeatureState.ItemsSource = filterFeatureStates;

            // Select the node that is hovered over when right clicking and before showing the context menu
            treeFilter.PreviewMouseRightButtonDown += TreeViewExtensions.TreeView_PreviewMouseRightButtonDown_SelectNode;
            RedrawFilterSection();
            RedrawFilterTreeView();
            RedrawMainMenu();
            LoadDimensions();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveDimensions();
        }

        private void LoadDimensions()
        {
            this.LoadWindowDimensions("Main_Window");
            gridMain.LoadGridDimensions("Main_Grid");
            gridFilterOptions.LoadGridDimensions("Main_GridFilterOptions");
            dataGridTasks.LoadDataGridDimensions("Main_GridTasks");
        }

        private void SaveDimensions()
        {
            this.SaveWindowDimensions("Main_Window");
            gridMain.SaveGridDimensions("Main_Grid");
            gridFilterOptions.SaveGridDimensions("Main_GridFilterOptions");
            dataGridTasks.SaveDataGridDimensions("Main_GridTasks");
        }

        private void RedrawFilterSection()
        {
            // Refresh task states listed in the task state filter list box
            filterTaskStates.Clear();
            var allTaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll();
            var distinctStates = new HashSet<string>(allTaskStates.Select(s => s.DisplayName));
            foreach (var state in distinctStates.OrderBy(s => s))
            {
                // Don't default to listing Closed items
                // mcbtodo: Save these filter prefs in the data store options instead of having the hardcoded "Closed" here.
                if (state.Equals("Closed"))
                {
                    filterTaskStates.Add(new CheckBoxListItem<string>(state, false));
                }
                else
                {
                    filterTaskStates.Add(new CheckBoxListItem<string>(state, true));
                }
            }

            // Refresh feature states listed in the filter list box
            filterFeatureStates.Clear();
            var internalSystem = CMDataProvider.DataStore.Value.CMSystems.Value.Get_InternalSystem();
            var allFeatureStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll()
                .Where(s => s.CMSystemId != internalSystem.Id); // Don't list the internal feature states in the UI
            var distinctFeatureStates = new HashSet<string>(allFeatureStates.Select(s => s.Name));
            foreach (var state in distinctFeatureStates.OrderBy(s => s))
            {
                filterFeatureStates.Add(new CheckBoxListItem<string>(state, true));
            }
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
                var cmSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystem.Id)
                    .OrderBy(s => s.MigrationOrder); // List the tree nodes in the same order that systems migrate to each other

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

            // Figure out which task states to show
            var filteredTaskStates = filterTaskStates.Where(lbi => lbi.IsSelected).Select(lbi => lbi.ObjectData);

            // Figure out which feature states to show
            var filteredFeatureStates = filterFeatureStates.Where(lbi => lbi.IsSelected).Select(lbi => lbi.ObjectData);

            // If there is a text filter
            var stringFilter = txtStringFilter.Text;

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

                // Filter to just tasks that have one of the feature states that are checked from the filter states listbox
                if (!filteredFeatureStates.Contains(featureSystemStateRef.Name))
                {
                    continue;
                }

                var taskSystemStateRef = systemStatesLookup[cmTask.CMSystemStateId];
                var taskStateRef = taskStatesLookup[cmTask.CMTaskStateId];

                // Filter to just tasks that have one of the states that are checked from the filter listbox
                if (!filteredTaskStates.Contains(taskStateRef.DisplayName))
                {
                    continue;
                }

                var taskTypeRef = taskTypesLookup[cmTask.CMTaskTypeId];

                // Apply string filter if it is set
                if (!string.IsNullOrWhiteSpace(stringFilter))
                {
                    if (
                        featureRef.Name.IndexOf(stringFilter, StringComparison.OrdinalIgnoreCase) >= 0
                        || cmTask.Title.IndexOf(stringFilter, StringComparison.OrdinalIgnoreCase) >= 0
                        || systemRef.Name.IndexOf(stringFilter, StringComparison.OrdinalIgnoreCase) >= 0
                        )
                    {
                        // String was found, keep this entry in the found results
                    }
                    else
                    {
                        continue;
                    }
                }

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
                    TaskRowBackgroundColor = featureRef.TasksBackgroundColor,
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
                    var newFeatureDialog = new FeatureCreator(cmFeatureTemplate);
                    newFeatureDialog.ShowDialog();

                    var newFeature = newFeatureDialog.CreatedFeature;

                    if (newFeature != null)
                    {
                        // Show any new tasks after the feature has been created
                        ShowFilteredTasks();
                        new FeatureEditor(newFeature).Show();
                    }
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
            cmFeatureTVI.ContextMenu = contextMenu;

            var openFeatureMenu = new MenuItem()
            {
                Header = "Open Feature"
            };
            contextMenu.Items.Add(openFeatureMenu);
            openFeatureMenu.Click += (sender, e) =>
            {
                new FeatureEditor(cmFeature).Show();
            };

            return cmFeatureTVI;
        }

        /// <summary>
        /// Call this after the data store path has been set, or verified that it is set after startup.
        /// It takes care of registering everything for the new or pre-existing data store
        /// </summary>
        private void DataStorePathSet()
        {
            // Do any upgrades or maintenance against the db.
            if (!DBMaintenance.RunMaintenanceRoutines())
            {
                this.Close();
            }

            var registerError = TaskFactories.Init();
            if (!string.IsNullOrWhiteSpace(registerError))
            {
                // If there is any errors with registering the task types then close the main window
                // Assuming that this means the program will not be able to run successfully without the property tasks registered
                MessageBox.Show(registerError);
                this.Close();
                return;
            }
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
            CMDataProvider.DataStore.Value.CMTasks.Value.OnBeforeRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnBeforeRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMTaskTypes.Value.OnBeforeRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordCreated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordUpdated += Record_CUD_StateCalcLookupsRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnBeforeRecordDeleted += Record_CUD_StateCalcLookupsRefreshNeeded;

            // All things that should result in the re-calculation of *all* current feature system states
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordCreated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnRecordUpdated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.OnBeforeRecordDeleted += Record_CUD_CalculateAllFeatureStates;

            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_CUD_CalculateAllFeatureStates;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnBeforeRecordDeleted += Record_CUD_CalculateAllFeatureStates;

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
            CMDataProvider.DataStore.Value.CMSystems.Value.OnBeforeRecordDeleted += Record_CUD_FilterTreeViewRefreshNeeded;

            CMDataProvider.DataStore.Value.CMSystemStates.Value.OnRecordCreated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMSystemStates.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMSystemStates.Value.OnBeforeRecordDeleted += Record_CUD_FilterTreeViewRefreshNeeded;

            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordCreated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnBeforeRecordDeleted += Record_CUD_FilterTreeViewRefreshNeeded;

            // Refresh the list of tasks when needed
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Record_CUD_FilterTreeViewRefreshNeeded;

            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordCreated += Record_CUD_RefreshFilteredTasks;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += Record_CUD_RefreshFilteredTasks;
            CMDataProvider.DataStore.Value.CMTasks.Value.OnBeforeRecordDeleted += Record_CUD_RefreshFilteredTasks;

            // Reload the datastore when the options are updated
            CMDataProvider.Master.Value.OnRecordCreated += Record_CUD_OptionsUpdated;
            CMDataProvider.Master.Value.OnRecordUpdated += Record_CUD_OptionsUpdated;
        }

        /// <summary>
        /// Triggered when the options are updated
        /// </summary>
        /// <param name="recordEventArgs"></param>
        private void Record_CUD_OptionsUpdated(object recordEventArgs)
        {
            if (DataStoreOptionConfigured())
            {
                DataStorePathSet();
            }
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
            var cmFilterData = ((FrameworkElement)sender).DataContext as FilterResultItem;
            if (cmFilterData == null)
            {
                return;
            }

            var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmFilterData.TaskId);

            var taskEditor = new TaskEditor(cmTask);

            // mcbtodo: keep track of open windows and just switch the already open one if it is, otherwise we open it here.
            taskEditor.Show();
        }

        private void btnViewFeature_Click(object sender, RoutedEventArgs e)
        {
            var cmFilterData = ((FrameworkElement)sender).DataContext as FilterResultItem;
            if (cmFilterData == null)
            {
                return;
            }

            var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmFilterData.TaskId);
            var cmFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmTask.CMFeatureId);

            var featureEditor = new FeatureEditor(cmFeature);
            featureEditor.Show();
        }

        private void lstFilterByTaskState_Checked(object sender, RoutedEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void lstFilterByTaskState_UnChecked(object sender, RoutedEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void lstFilterByFeatureState_Checked(object sender, RoutedEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void lstFilterByFeatureState_UnChecked(object sender, RoutedEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void txtStringFilter_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ShowFilteredTasks();
        }

        private void chkFilterTaskStates_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var taskState in filterTaskStates)
            {
                taskState.IsSelected = true;
            }
            lstFilterByTaskState.ItemsSource = null;
            lstFilterByTaskState.ItemsSource = filterTaskStates;
            ShowFilteredTasks();
        }

        private void chkFilterTaskStates_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var taskState in filterTaskStates)
            {
                taskState.IsSelected = false;
            }
            lstFilterByTaskState.ItemsSource = null;
            lstFilterByTaskState.ItemsSource = filterTaskStates;
            ShowFilteredTasks();
        }

        private void chkFilterFeatureStates_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var featureState in filterFeatureStates)
            {
                featureState.IsSelected = true;
            }
            lstFilterByFeatureState.ItemsSource = null;
            lstFilterByFeatureState.ItemsSource = filterFeatureStates;
            ShowFilteredTasks();
        }

        private void chkFilterFeatureStates_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var featureState in filterFeatureStates)
            {
                featureState.IsSelected = false;
            }
            lstFilterByFeatureState.ItemsSource = null;
            lstFilterByFeatureState.ItemsSource = filterFeatureStates;
            ShowFilteredTasks();
        }
    }
}
