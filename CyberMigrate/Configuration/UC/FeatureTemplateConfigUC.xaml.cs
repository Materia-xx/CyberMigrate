using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CyberMigrate.ConfigurationUC
{
    /// <summary>
    /// Interaction logic for FeatureTemplateConfigUC.xaml
    /// </summary>
    public partial class FeatureTemplateConfigUC : UserControl
    {
        private CMFeatureDto cmFeatureTemplate;

        private Config ConfigWindow { get; set; }

        private List<BoolBasedComboBoxEntry> ConditionAllAnyChoices { get; set; }

        private List<BoolBasedComboBoxEntry> ConditionAreCompleteChoices { get; set; }

        private List<CMSystemStateDto> FeatureTemplateSystemStates { get; set; }

        private List<CMTaskTypeDto> TaskTypes { get; set; }

        public FeatureTemplateConfigUC(Config configWindow, CMFeatureDto cmFeatureTemplate)
        {
            InitializeComponent();
            this.cmFeatureTemplate = cmFeatureTemplate;
            this.ConfigWindow = configWindow;
        }

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

        private List<CMSystemStateDto> CurrentSystemStates { get; set; }

        /// <summary>
        /// Loads the lists that will be displayed as dropdown choices in the task templates datagrid
        /// </summary>
        private void LoadComboBoxClasses_TaskTemplates()
        {
            FeatureTemplateSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(cmFeatureTemplate.Id).ToList();

            TaskTypes = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll().ToList(); ;
        }

        /// <summary>
        /// Loads the lists that will be displayed as dropdown choices in the state transitions datagrid
        /// </summary>
        private void LoadComboBoxClasses_TransitionRules()
        {
            ConditionAllAnyChoices = new List<BoolBasedComboBoxEntry>()
            {
                new BoolBasedComboBoxEntry(true, "All"),
                new BoolBasedComboBoxEntry(false, "Any")
            };

            ConditionAreCompleteChoices = new List<BoolBasedComboBoxEntry>()
            {
                new BoolBasedComboBoxEntry(true, "Are complete"),
                new BoolBasedComboBoxEntry(false, "Are not complete")
            };

            CurrentSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmFeatureTemplate.CMSystemId).ToList();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtFeatureTemplateName.Text = cmFeatureTemplate.Name;
            Load_TransitionRulesGrid();
            Load_TaskTemplatesGrid();
        }

        private void Load_TransitionRulesGrid()
        {
            LoadComboBoxClasses_TransitionRules();

            dataGridStateTransitionRules.AutoGenerateColumns = false;
            dataGridStateTransitionRules.Columns.Clear();

            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMFeatureStateTransitionRuleDto.Id),
                    Binding = new Binding(nameof(CMFeatureStateTransitionRuleDto.Id)),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
            // Note: CMFeatureId is always set to the current editor feature id when saving, there isn't a need for it here in a non-visible column
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMFeatureStateTransitionRuleDto.Priority),
                    Binding = new Binding(nameof(CMFeatureStateTransitionRuleDto.Priority)),
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "If (all / any) tasks",
                    Width = 150,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ConditionAllTasks)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = ConditionAllAnyChoices,
                    SelectedValuePath = nameof(BoolBasedComboBoxEntry.Value),
                    DisplayMemberPath = nameof(BoolBasedComboBoxEntry.Name),
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "In state",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ConditionQuerySystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = CurrentSystemStates,
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                });

            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "Are ...",
                    Width = 150,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ConditionTaskComplete)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = ConditionAreCompleteChoices,
                    SelectedValuePath = nameof(BoolBasedComboBoxEntry.Value),
                    DisplayMemberPath = nameof(BoolBasedComboBoxEntry.Name),
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "Then move to state",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ToCMSystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = CurrentSystemStates,
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                });

            // Load all state transition rules
            var cmFeatureStateTransitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(cmFeatureTemplate.Id).ToList();
            var observable = new ObservableCollection<CMFeatureStateTransitionRuleDto>(cmFeatureStateTransitionRules);
            observable.CollectionChanged += FeatureStateTransitionRules_CollectionChanged;
            dataGridStateTransitionRules.ItemsSource = observable;

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridStateTransitionRules.RowEditEnding -= DataGridStateTransitionRules_RowEditEnding;
            dataGridStateTransitionRules.RowEditEnding += DataGridStateTransitionRules_RowEditEnding;
        }

        private void Load_TaskTemplatesGrid()
        {
            LoadComboBoxClasses_TaskTemplates();

            dataGridTaskTemplates.AutoGenerateColumns = false;
            dataGridTaskTemplates.Columns.Clear();

            dataGridTaskTemplates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMTaskDto.Id),
                    Binding = new Binding(nameof(CMTaskDto.Id)),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
            // Note: CMFeatureId is always set to the current editor feature id when saving, there isn't a need for it here in a non-visible column
            // Note: Same with IsTemplate
            dataGridTaskTemplates.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "System State",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMTaskDto.CMSystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = FeatureTemplateSystemStates,
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                });
            dataGridTaskTemplates.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "Task Type",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMTaskDto.CMTaskTypeId)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = TaskTypes,
                    SelectedValuePath = nameof(CMTaskTypeDto.Id),
                    DisplayMemberPath = nameof(CMTaskTypeDto.Name),
                });
            dataGridTaskTemplates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMTaskDto.Name),
                    Width = 400,
                    Binding = new Binding(nameof(CMTaskDto.Name)),
                });

            // A factory because each row will generate a button
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty, "Edit");
            editButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(btnEditTask_Click));
            dataGridTaskTemplates.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Edit Task Template",
                CellTemplate = new DataTemplate()
                {
                    VisualTree = editButtonFactory
                }
            });

            var cmTaskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(cmFeatureTemplate.Id, true).ToList();

            var observable = new ObservableCollection<CMTaskDto>(cmTaskTemplates);
            observable.CollectionChanged += TaskTemplates_CollectionChanged;
            dataGridTaskTemplates.ItemsSource = observable;

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridTaskTemplates.RowEditEnding -= DataGridTaskTemplates_RowEditEnding;
            dataGridTaskTemplates.RowEditEnding += DataGridTaskTemplates_RowEditEnding;
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
                        Load_TransitionRulesGrid();
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

                // Reload the tasks grid so the dropdowns now represent the correct system states that are available.
                // Just in case the update changed an availalbe system state
                Load_TaskTemplatesGrid();
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
                        Load_TransitionRulesGrid();
                        return;
                    }

                    // The row will already be correctly removed from the rules datagrid so no need at this point to refresh the rules grid.
                    // However we reload the tasks grid so the dropdowns now represent the correct system states that are available.
                    Load_TaskTemplatesGrid();
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
                    gridRule.CMFeatureId = cmFeatureTemplate.Id;
                }
            }
        }

        private void DataGridTaskTemplates_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Unfortunately, I'm unable to find any reference to the "new values" as a Dto object.
                // I imagine it would be possible to dig through the cells and construct it, but IMO that is even worse than this:
                dataGridTaskTemplates.RowEditEnding -= DataGridTaskTemplates_RowEditEnding;
                dataGridTaskTemplates.CommitEdit();
                dataGridTaskTemplates.Items.Refresh();
                dataGridTaskTemplates.RowEditEnding += DataGridTaskTemplates_RowEditEnding;

                var gridTask = (CMTaskDto)dataGridTaskTemplates.SelectedItem;

                // If the item already exists in the db
                if (gridTask.Id > 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Update(gridTask);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the tasks grid
                        Load_TaskTemplatesGrid();
                        return;
                    }
                }
                else
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Insert(gridTask);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Keep the incorrect row in the grid so they can keep trying to make it correct
                        return;
                    }
                }
            }
        }

        private void TaskTemplates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removedTask in e.OldItems)
                {
                    var gridTask = (CMTaskDto)removedTask;

                    // This task may have never actually been added to the db because it was a new row that didn't yet meet the db requirements
                    // So make sure it has a valid id first before trying to delete it.
                    if (gridTask.Id > 0)
                    {
                        var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Delete(gridTask.Id);
                        if (opResult.Errors.Any())
                        {
                            MessageBox.Show(opResult.ErrorsCombined);
                            // Reload the tasks datagrid to show that the item was not actually deleted
                            Load_TaskTemplatesGrid();
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
                //  * A new CMTaskDto is constructed and added to the observable collection.
                //    Note that at this point this Dto is not in a valid state to be entered into the db and an insert operation will fail.
                // Therefore we do not do the insert attempt at this point. Instead it is handled in the row update code.
                // However we do set defaults for things here that won't be available to set through the grid UI
                foreach (var addedTask in e.NewItems)
                {
                    var gridTask = (CMTaskDto)addedTask;
                    gridTask.CMFeatureId = cmFeatureTemplate.Id;
                    gridTask.IsTemplate = true;
                }
            }
        }

        private void btnEditTask_Click(object sender, RoutedEventArgs e)
        {
            var selectedRowIndex = dataGridTaskTemplates.SelectedIndex;
            var cmTaskTemplates = (ObservableCollection<CMTaskDto>)dataGridTaskTemplates.ItemsSource;

            if (cmTaskTemplates.Count() <= selectedRowIndex)
            {
                // This means clicking on the button of a new row that has not yet been added into the database
                return;
            }

            var cmTask = cmTaskTemplates[selectedRowIndex];
            
            // mcbtodo: add logic to show the task editor for cmTask
        }

        private void txtFeatureTemplateName_LostFocus(object sender, RoutedEventArgs e)
        {
            // Update the feature template. Load it first from the db first just in case it has been updated elsewhere.
            var cmFeatureTemplateDb = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmFeatureTemplate.Id);
            var originalName = cmFeatureTemplateDb.Name;
            cmFeatureTemplateDb.Name = txtFeatureTemplateName.Text;

            // If the name wasn't actually changed, then there is no need to try and update
            if (originalName.Equals(cmFeatureTemplateDb.Name, StringComparison.Ordinal)) // Note: case 'sensitive' compare so we allow renames to upper/lower case
            {
                return;
            }

            var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Update(cmFeatureTemplateDb);
            if (opResult.Errors.Any())
            {
                MessageBox.Show(opResult.ErrorsCombined);
                txtFeatureTemplateName.Text = originalName;
                return;
            }

            // Reload main treeview, this is how we handle renames
            cmFeatureTemplate = cmFeatureTemplateDb;
            ConfigWindow.ReLoadTreeConfiguration();
        }
    }
}
