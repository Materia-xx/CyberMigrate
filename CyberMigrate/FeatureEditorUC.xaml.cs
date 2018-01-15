using DataProvider;
using DataProvider.Events;
using Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TaskBase.Extensions;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for FeatureEditorUC.xaml
    /// </summary>
    public partial class FeatureEditorUC : UserControl
    {
        private CMFeatureDto cmFeatureDto;

        /// <summary>
        /// A ref to the window that is hosting this user control
        /// </summary>
        private Window parentWindow;

        private List<CMSystemStateDto> ComboBox_FeatureTemplateSystemStates { get; set; } = new List<CMSystemStateDto>();

        private List<CMTaskTypeDto> ComboBox_TaskTypes { get; set; } = new List<CMTaskTypeDto>();

        private List<CMSystemStateDto> ComboBox_CurrentSystemStates { get; set; } = new List<CMSystemStateDto>();

        private ObservableCollection<CMFeatureStateTransitionRuleDto> featureStateTransitionRules = new ObservableCollection<CMFeatureStateTransitionRuleDto>();

        private ObservableCollection<CMFeatureVarStringDto> featureVariables = new ObservableCollection<CMFeatureVarStringDto>();

        private ObservableCollection<FeatureEditorTaskRowDto> tasks = new ObservableCollection<FeatureEditorTaskRowDto>();

        private class BoolBasedComboBoxEntry
        {
            public BoolBasedComboBoxEntry(bool value, string name)
            {
                this.Value = value;
                this.Name = name;
            }

            public bool Value { get; private set; }
            public string Name { get; private set; }
        }

        public FeatureEditorUC(CMFeatureDto cmFeature, Window parentWindow)
        {
            InitializeComponent();
            this.cmFeatureDto = cmFeature;
            this.parentWindow = parentWindow;

            // Updating a task reloads the task list if the task state changes
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordUpdated += TaskRecord_Updated_ReloadTaskList;
        }

        private void TaskRecord_Updated_ReloadTaskList(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            if (tasks == null)
            {
                return;
            }

            var after = updatedRecordEventArgs.DtoAfter as CMTaskDto;
            var before = updatedRecordEventArgs.DtoBefore as CMTaskDto;
            // If the state of the task that is being updated is not changing then we don't need to reload the rows
            // because the only reason to reload the rows is to get the row coloring to show up correctly at the time of the change
            if (after?.CMTaskStateId == before?.CMTaskStateId)
            {
                return;
            }

            Reload_Tasks();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtFeatureName.Text = cmFeatureDto.Name;
            txtFeatureDescription.Text = cmFeatureDto.Description;

            // Load different parts depending on if it is a feature instance or template
            if (cmFeatureDto.IsTemplate)
            {
                lblFeatureVars.Visibility = Visibility.Hidden;
                dataGridFeatureVars.Visibility = Visibility.Hidden;

                lblStateTransitionRules.Visibility = Visibility.Visible;
                dataGridStateTransitionRules.Visibility = Visibility.Visible;

                lblFeatureName.Content = "Feature Template Name";
                lblTasks.Content = "Task Templates";

                Init_TransitionRulesGrid();
            }
            else
            {
                lblFeatureVars.Visibility = Visibility.Visible;
                dataGridFeatureVars.Visibility = Visibility.Visible;

                lblStateTransitionRules.Visibility = Visibility.Hidden;
                dataGridStateTransitionRules.Visibility = Visibility.Hidden;

                lblFeatureName.Content = "Feature Name";
                lblTasks.Content = "Tasks";

                Init_FeatureVarsGrid();
            }

            Init_TasksGrid();
        }

        /// <summary>
        /// Loads the lists that will be displayed as dropdown choices in the tasks datagrid
        /// </summary>
        private void LoadComboBoxes_Tasks()
        {
            ComboBox_FeatureTemplateSystemStates.Clear();

            var featureTemplateId = cmFeatureDto.IsTemplate ? cmFeatureDto.Id : cmFeatureDto.CMParentFeatureTemplateId;

            ComboBox_FeatureTemplateSystemStates.AddRange(
                CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(featureTemplateId)
                );

            ComboBox_TaskTypes.Clear();
            ComboBox_TaskTypes.AddRange(
                CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll()
                );
        }

        /// <summary>
        /// Loads the lists that will be displayed as dropdown choices in the state transitions datagrid
        /// </summary>
        private void LoadComboBoxes_TransitionRules()
        {
            ComboBox_CurrentSystemStates.Clear();
            ComboBox_CurrentSystemStates.AddRange(
                CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmFeatureDto.CMSystemId)
                .OrderBy(s => s.MigrationOrder)
                );
        }

        private void Init_TransitionRulesGrid()
        {
            LoadComboBoxes_TransitionRules();

            dataGridStateTransitionRules.AutoGenerateColumns = false;
            dataGridStateTransitionRules.Columns.Clear();

            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMFeatureStateTransitionRuleDto.Order),
                    Binding = new Binding(nameof(CMFeatureStateTransitionRuleDto.Order)),
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "System State",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.CMSystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = ComboBox_CurrentSystemStates,
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                });

            // Load all state transition rules
            dataGridStateTransitionRules.ItemsSource = featureStateTransitionRules;
            Reload_FeatureStateTransitionRules();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridStateTransitionRules.RowEditEnding += DataGridStateTransitionRules_RowEditEnding;
        }

        private void Init_TasksGrid()
        {
            LoadComboBoxes_Tasks();

            dataGridTasks.AutoGenerateColumns = false;
            dataGridTasks.Columns.Clear();

            dataGridTasks.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "System State",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding($"{nameof(FeatureEditorTaskRowDto.Task)}.{nameof(CMTaskDto.CMSystemStateId)}"),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = ComboBox_FeatureTemplateSystemStates,
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                });
            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Order",
                    Width = 50,
                    Binding = new Binding($"{nameof(FeatureEditorTaskRowDto.Task)}.{nameof(CMTaskDto.ExecutionOrder)}"),
                });
            dataGridTasks.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "Task Type",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding($"{nameof(FeatureEditorTaskRowDto.Task)}.{nameof(CMTaskDto.CMTaskTypeId)}"),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = ComboBox_TaskTypes,
                    SelectedValuePath = nameof(CMTaskTypeDto.Id),
                    DisplayMemberPath = nameof(CMTaskTypeDto.Name),
                });
            dataGridTasks.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMTaskDto.Title),
                    Width = 400,
                    Binding = new Binding($"{nameof(FeatureEditorTaskRowDto.Task)}.{nameof(CMTaskDto.Title)}"),
                });

            // A factory because each row will generate a button
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty,
                new Binding($"{nameof(FeatureEditorTaskRowDto.EditTaskDataButtonText)}"));

            editButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(btnEditTask_Click));
            dataGridTasks.Columns.Add(new DataGridTemplateColumn
            {
                Header = cmFeatureDto.IsTemplate ? "Edit Task Template" : "Edit Task",
                CellTemplate = new DataTemplate()
                {
                    VisualTree = editButtonFactory
                }
            });

            // Load all task templates
            dataGridTasks.ItemsSource = tasks;
            Reload_Tasks();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridTasks.RowEditEnding += DataGridTasks_RowEditEnding;
        }

        private void Init_FeatureVarsGrid()
        {
            dataGridFeatureVars.ItemsSource = featureVariables;
            Reload_FeatureVariables();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridFeatureVars.RowEditEnding += DataGridFeatureVariables_RowEditEnding;
        }

        private void Reload_FeatureStateTransitionRules()
        {
            featureStateTransitionRules.CollectionChanged -= FeatureStateTransitionRules_CollectionChanged;
            featureStateTransitionRules.Clear();
            var cmFeatureStateTransitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(cmFeatureDto.Id).ToList();
            foreach (var rule in cmFeatureStateTransitionRules)
            {
                featureStateTransitionRules.Add(rule);
            }
            featureStateTransitionRules.CollectionChanged += FeatureStateTransitionRules_CollectionChanged;
        }

        private void Reload_Tasks()
        {
            tasks.CollectionChanged -= Tasks_CollectionChanged;
            tasks.Clear();
            var cmTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(cmFeatureDto.Id).ToList();
            foreach (var taskDto in cmTasks)
            {
                tasks.Add(new FeatureEditorTaskRowDto(taskDto));
            }
            tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        private void Reload_FeatureVariables()
        {
            featureVariables.CollectionChanged -= FeatureVariables_CollectionChanged;
            featureVariables.Clear();
            var cmFeatureVariables = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(cmFeatureDto.Id).ToList();
            foreach (var featureVar in cmFeatureVariables)
            {
                featureVariables.Add(featureVar);
            }

            featureVariables.CollectionChanged += FeatureVariables_CollectionChanged;
        }

        private void DataGridStateTransitionRules_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Unfortunately, I'm unable to find any reference to the "new values" as a Dto object.
                // I imagine it would be possible to dig through the cells and construct it, but IMO that is even worse than this:
                dataGridStateTransitionRules.RowEditEnding -= DataGridStateTransitionRules_RowEditEnding;
                dataGridStateTransitionRules.CommitEdit();
                dataGridStateTransitionRules.Items.Refresh();
                dataGridStateTransitionRules.RowEditEnding += DataGridStateTransitionRules_RowEditEnding;

                var gridRule = (CMFeatureStateTransitionRuleDto)dataGridStateTransitionRules.SelectedItem;

                // If the item already exists in the db
                if (gridRule.Id > 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Update(gridRule);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the rules grid
                        Reload_FeatureStateTransitionRules();
                        return;
                    }
                }
                else
                {
                    var opResult = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Insert(gridRule);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Keep the incorrect row in the grid so they can keep trying to make it correct
                        return;
                    }
                }

                // Reload the tasks grid combo boxes to now represent the correct system states that are available.
                LoadComboBoxes_Tasks();
            }
        }

        private void DataGridTasks_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Unfortunately, I'm unable to find any reference to the "new values" as a Dto object.
                // I imagine it would be possible to dig through the cells and construct it, but IMO that is even worse than this:
                dataGridTasks.RowEditEnding -= DataGridTasks_RowEditEnding;
                dataGridTasks.CommitEdit();
                dataGridTasks.Items.Refresh();
                dataGridTasks.RowEditEnding += DataGridTasks_RowEditEnding;

                var gridTask = (FeatureEditorTaskRowDto)dataGridTasks.SelectedItem;

                // Update the task to be in the Template state if a task type is selected (and the editor is editing a feature template)
                if (cmFeatureDto.IsTemplate && gridTask.Task.CMTaskTypeId > 0)
                {
                    var selectedTaskType = ComboBox_TaskTypes.Single(t => t.Id == gridTask.Task.CMTaskTypeId);
                    gridTask.Task.CMTaskStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Template, selectedTaskType.Id).Id;
                }

                // If the item already exists in the db
                if (gridTask.Task.Id > 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Update(gridTask.Task);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the tasks grid
                        Reload_Tasks();
                        return;
                    }
                }
                else
                {
                    // If we are creating a task instance
                    if (!cmFeatureDto.IsTemplate)
                    {
                        if (gridTask.Task.CMTaskTypeId == 0)
                        {
                            MessageBox.Show("The task type must be set.");
                            return;
                        }

                        // All task instances must point back to the task template they were created from.
                        // Since ad-hoc tasks don't really have a template, we instead point it at a special internal ad-hoc task template.
                        var adhocTaskTemplate = CMDataProvider.DataStore.Value.CMTasks.Value.Get_AdHocTemplate(gridTask.Task.CMTaskTypeId);

                        gridTask.Task.CMParentTaskTemplateId = adhocTaskTemplate.Id;
                        gridTask.Task.CMTaskStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Instance, adhocTaskTemplate.CMTaskTypeId).Id;
                    }

                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Insert(gridTask.Task);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Keep the incorrect row in the grid so they can keep trying to make it correct
                        return;
                    }
                }
            }
        }

        private void DataGridFeatureVariables_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Unfortunately, I'm unable to find any reference to the "new values" as a Dto object.
                // I imagine it would be possible to dig through the cells and construct it, but IMO that is even worse than this:
                dataGridFeatureVars.RowEditEnding -= DataGridFeatureVariables_RowEditEnding;
                dataGridFeatureVars.CommitEdit();
                dataGridFeatureVars.Items.Refresh();
                dataGridFeatureVars.RowEditEnding += DataGridFeatureVariables_RowEditEnding;

                var gridFeatureVar = (CMFeatureVarStringDto)dataGridFeatureVars.SelectedItem;

                // If the item already exists in the db
                if (gridFeatureVar.Id > 0)
                {
                    // This will always show the message that feature vars are immutable

                    var opResult = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.Update(gridFeatureVar);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the grid
                        Reload_FeatureVariables();
                        return;
                    }
                }
                else
                {
                    var choice = MessageBox.Show($"Once a feature variable is set it cannot be changed.\r\nAre you sure you want to set this variable ?\r\n\r\nName: {gridFeatureVar.Name}\r\nValue:{gridFeatureVar.Value}", "Are you sure", MessageBoxButton.OKCancel);

                    if (choice == MessageBoxResult.OK)
                    {
                        var opResult = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.Insert(gridFeatureVar);
                        if (opResult.Errors.Any())
                        {
                            MessageBox.Show(opResult.ErrorsCombined);

                            // Keep the incorrect row in the grid so they can keep trying to make it correct
                            return;
                        }
                    }
                }
            }
        }

        private void FeatureStateTransitionRules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removedRule in e.OldItems)
                {
                    var gridRule = (CMFeatureStateTransitionRuleDto)removedRule;

                    var opResult = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Delete(gridRule.Id);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);
                        // Reload the rules datagrid to show that the item was not actually deleted
                        Reload_FeatureStateTransitionRules();
                        return;
                    }

                    // The row will already be correctly removed from the rules datagrid so no need at this point to refresh the rules grid.

                    // Reload the tasks grid combo boxes to now represent the correct system states that are available.
                    LoadComboBoxes_Tasks();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // The order of operations (I believe) is:
                //  * A new row is added to the datagrid
                //  * A new CMFeatureStateTransitionRuleDto is constructed and added to the observable collection.
                //    Note that at this point this Dto *may* not be in a valid state to be entered into the db and an insert operation will fail.
                // Therefore we do not do the insert attempt at this point. Instead it is handled in the row update code.
                // However we do set defaults for things here that won't be available to set through the grid UI
                foreach (var addedRule in e.NewItems)
                {
                    var gridRule = (CMFeatureStateTransitionRuleDto)addedRule;
                    gridRule.CMFeatureId = cmFeatureDto.Id;
                }
            }
        }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removedTask in e.OldItems)
                {
                    var gridTask = (FeatureEditorTaskRowDto)removedTask;

                    // This task may have never actually been added to the db because it was a new row that didn't yet meet the db requirements
                    // So make sure it has a valid id first before trying to delete it.
                    if (gridTask.Task.Id > 0)
                    {
                        var deletingTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(gridTask.Task.Id);

                        var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Delete(deletingTask.Id);
                        if (opResult.Errors.Any())
                        {
                            MessageBox.Show(opResult.ErrorsCombined);
                            // Reload the tasks datagrid to show that the item was not actually deleted
                            Reload_Tasks();
                            return;
                        }
                    }

                    // The row will already be correctly removed from the tasks datagrid so no need at this point to refresh the tasks grid.
                    // Also the rules do not depend on the tasks, so no need to reload that either.
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // The order of operations (I believe) is:
                //  * A new row is added to the datagrid
                //  * A new FeatureEditorTaskRowDto is constructed and added to the observable collection.
                //    Note that at this point this Dto is not in a valid state to be entered into the db and an insert operation will fail.
                // Therefore we do not do the insert attempt at this point. Instead it is handled in the row update code.
                // However we do set defaults for things here that won't be available to set through the grid UI
                foreach (var addedTask in e.NewItems)
                {
                    var gridTask = (FeatureEditorTaskRowDto)addedTask;
                    gridTask.Task.CMFeatureId = cmFeatureDto.Id;
                }
            }
        }

        private void FeatureVariables_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removedFeatureVar in e.OldItems)
                {
                    var gridFeatureVar = (CMFeatureVarStringDto)removedFeatureVar;

                    // This var may have never actually been added to the db because it was a new row that didn't yet meet the db requirements
                    // So make sure it has a valid id first before trying to delete it.
                    if (gridFeatureVar.Id > 0)
                    {
                        var deletingFeatureVar = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.Get(gridFeatureVar.Id);

                        var opResult = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.Delete(deletingFeatureVar.Id);
                        if (opResult.Errors.Any())
                        {
                            MessageBox.Show(opResult.ErrorsCombined);
                            // Reload the feature vars datagrid to show that the item was not actually deleted
                            Reload_FeatureVariables();
                            return;
                        }
                    }

                    // The row will already be correctly removed from the feature vars datagrid so no need at this point to refresh the grid.
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // The order of operations (I believe) is:
                //  * A new row is added to the datagrid
                //  * A new CMFeatureVarStringDto is constructed and added to the observable collection.
                //    Note that at this point this Dto is not in a valid state to be entered into the db and an insert operation will fail.
                // Therefore we do not do the insert attempt at this point. Instead it is handled in the row update code.
                // However we do set defaults for things here that won't be available to set through the grid UI
                foreach (var addedFeatureVar in e.NewItems)
                {
                    var gridFeatureVar = (CMFeatureVarStringDto)addedFeatureVar;
                    gridFeatureVar.CMFeatureId = cmFeatureDto.Id;
                }
            }
        }

        private void btnEditTask_Click(object sender, RoutedEventArgs e)
        {
            var cmTask = ((FrameworkElement)sender).DataContext as FeatureEditorTaskRowDto;
            if (cmTask?.Task == null || cmTask.Task.Id == 0)
            {
                // Null: This means clicking on the button of a new row that has not yet been added into the database
                // ==0:  Also a new row not yet in the db, but in a further state than the above.
                MessageBox.Show("A task must be fully entered before the editor can be invoked.");
                return;
            }

            // Currently we only allow 1 task editor template to be present at a time
            var taskEditor = new TaskEditor(cmTask.Task);
            taskEditor.ShowDialog();
            // Reload the task rows. Without this the buttons won't refresh their text.
            Reload_Tasks();
        }

        private void txtFeatureName_LostFocus(object sender, RoutedEventArgs e)
        {
            var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.UpdateIfNeeded_Name(cmFeatureDto.Id, txtFeatureName.Text);
            if (opResult.Errors.Any())
            {
                MessageBox.Show(opResult.ErrorsCombined);
                txtFeatureName.Text = cmFeatureDto.Name;
                return;
            }

            cmFeatureDto = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmFeatureDto.Id);
            if (parentWindow != null)
            {
                parentWindow.Title = cmFeatureDto.Name;
            }
        }

        private void txtFeatureDescription_LostFocus(object sender, RoutedEventArgs e)
        {
            var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.UpdateIfNeeded_Description(cmFeatureDto.Id, txtFeatureDescription.Text);
            if (opResult.Errors.Any())
            {
                MessageBox.Show(opResult.ErrorsCombined);
                txtFeatureDescription.Text = cmFeatureDto.Description;
                return;
            }

            cmFeatureDto = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmFeatureDto.Id);
        }
    }
}
