using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CyberMigrate.Configuration
{
    /// <summary>
    /// Interaction logic for FeatureTemplateConfigUC.xaml
    /// </summary>
    public partial class FeatureTemplateConfigUC : UserControl
    {
        private CMFeatureDto cmFeatureTemplate;

        private Config ConfigWindow { get; set; }

        private List<BoolBasedComboBoxEntry> ComboBox_ConditionAllAnyChoices { get; set; }

        private List<BoolBasedComboBoxEntry> ComboBox_ConditionAreCompleteChoices { get; set; }

        private List<CMSystemStateDto> ComboBox_FeatureTemplateSystemStates { get; set; }

        private List<CMTaskTypeDto> ComboBox_TaskTypes { get; set; }

        private List<CMSystemStateDto> ComboBox_CurrentSystemStates { get; set; }

        private ObservableCollection<CMFeatureStateTransitionRuleDto> featureStateTransitionRules = new ObservableCollection<CMFeatureStateTransitionRuleDto>();

        private ObservableCollection<CMTaskDto> taskTemplates = new ObservableCollection<CMTaskDto>();

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

        public FeatureTemplateConfigUC(Config configWindow, CMFeatureDto cmFeatureTemplate)
        {
            InitializeComponent();
            this.cmFeatureTemplate = cmFeatureTemplate;
            this.ConfigWindow = configWindow;
        }

        /// <summary>
        /// Loads the lists that will be displayed as dropdown choices in the task templates datagrid
        /// </summary>
        private void LoadComboBoxes_TaskTemplates()
        {
            ComboBox_FeatureTemplateSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(cmFeatureTemplate.Id).ToList();
            ComboBox_TaskTypes = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll().ToList(); ;
        }

        /// <summary>
        /// Loads the lists that will be displayed as dropdown choices in the state transitions datagrid
        /// </summary>
        private void LoadComboBoxes_TransitionRules()
        {
            ComboBox_ConditionAllAnyChoices = new List<BoolBasedComboBoxEntry>()
            {
                new BoolBasedComboBoxEntry(true, "All"),
                new BoolBasedComboBoxEntry(false, "Any")
            };

            ComboBox_ConditionAreCompleteChoices = new List<BoolBasedComboBoxEntry>()
            {
                new BoolBasedComboBoxEntry(true, "Are complete"),
                new BoolBasedComboBoxEntry(false, "Are not complete")
            };

            ComboBox_CurrentSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmFeatureTemplate.CMSystemId).ToList();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtFeatureTemplateName.Text = cmFeatureTemplate.Name;
            Init_TransitionRulesGrid();
            Init_TaskTemplatesGrid();
        }

        private void Init_TransitionRulesGrid()
        {
            LoadComboBoxes_TransitionRules();

            dataGridStateTransitionRules.AutoGenerateColumns = false;
            dataGridStateTransitionRules.Columns.Clear();

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
                    ItemsSource = ComboBox_ConditionAllAnyChoices,
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
                    ItemsSource = ComboBox_CurrentSystemStates,
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
                    ItemsSource = ComboBox_ConditionAreCompleteChoices,
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

        private void Init_TaskTemplatesGrid()
        {
            LoadComboBoxes_TaskTemplates();

            dataGridTaskTemplates.AutoGenerateColumns = false;
            dataGridTaskTemplates.Columns.Clear();

            dataGridTaskTemplates.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "System State",
                    Width = 200,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMTaskDto.CMSystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    ItemsSource = ComboBox_FeatureTemplateSystemStates,
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
                    ItemsSource = ComboBox_TaskTypes,
                    SelectedValuePath = nameof(CMTaskTypeDto.Id),
                    DisplayMemberPath = nameof(CMTaskTypeDto.Name),
                });
            dataGridTaskTemplates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMTaskDto.Title),
                    Width = 400,
                    Binding = new Binding(nameof(CMTaskDto.Title)),
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

            // Load all task templates
            dataGridTaskTemplates.ItemsSource = taskTemplates;
            Reload_TaskTemplates();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridTaskTemplates.RowEditEnding += DataGridTaskTemplates_RowEditEnding;
        }

        private void Reload_FeatureStateTransitionRules()
        {
            featureStateTransitionRules.CollectionChanged -= FeatureStateTransitionRules_CollectionChanged;
            featureStateTransitionRules.Clear();
            var cmFeatureStateTransitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(cmFeatureTemplate.Id).ToList();
            foreach (var rule in cmFeatureStateTransitionRules)
            {
                featureStateTransitionRules.Add(rule);
            }
            featureStateTransitionRules.CollectionChanged += FeatureStateTransitionRules_CollectionChanged;
        }

        private void Reload_TaskTemplates()
        {
            taskTemplates.CollectionChanged -= TaskTemplates_CollectionChanged;
            taskTemplates.Clear();
            var cmTaskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(cmFeatureTemplate.Id, true).ToList();
            foreach (var taskTemplate in cmTaskTemplates)
            {
                taskTemplates.Add(taskTemplate);
            }
            taskTemplates.CollectionChanged += TaskTemplates_CollectionChanged;
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

                // Reload the tasks grid so the dropdowns now represent the correct system states that are available.
                // Just in case the update changed an availalbe system state
                Reload_TaskTemplates();
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
                    // However we reload the tasks grid so the dropdowns now represent the correct system states that are available.
                    Reload_TaskTemplates();
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

                // Update the task to be in the Template state if a task type is selected
                if (gridTask.CMTaskTypeId > 0)
                {
                    var selectedTaskType = ComboBox_TaskTypes.Single(t => t.Id == gridTask.CMTaskTypeId);
                    gridTask.CMTaskStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(CMTaskStatesCRUD.InternalState_Template, selectedTaskType.Id).Id;
                }

                // If the item already exists in the db
                if (gridTask.Id > 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Update(gridTask);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the tasks grid
                        Reload_TaskTemplates();
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
                            Reload_TaskTemplates();
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
                MessageBox.Show("A task must be fully entered before the editor can be invoked.");
                return;
            }

            var cmTask = cmTaskTemplates[selectedRowIndex];
            if (cmTask.Id == 0)
            {
                // Also a new row not yet in the db, but in a further state than the above.
                MessageBox.Show("A task must be fully entered before the editor can be invoked.");
                return;
            }

            // Currently we only allow 1 task editor template to be present at a time
            var taskEditor = new TaskEditor(cmTask);
            taskEditor.ShowDialog();
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
