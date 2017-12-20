using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CyberMigrate.ConfigurationUC
{
    /// <summary>
    /// Interaction logic for FeatureTemplateConfigUC.xaml
    /// </summary>
    public partial class FeatureTemplateConfigUC : UserControl
    {
        private CMFeatureTemplateDto cmFeatureTemplate;

        private Config ConfigWindow { get; set; }

        private List<BoolBasedComboBoxEntry> ConditionAllAnyChoices { get; set; }

        private List<BoolBasedComboBoxEntry> ConditionAreCompleteChoices { get; set; }

        public FeatureTemplateConfigUC(Config configWindow, CMFeatureTemplateDto cmFeatureTemplate)
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
        /// Load the lists that will be displayed as dropdown choices in the state transitions datagrid
        /// </summary>
        private void LoadComboBoxClasses()
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

            CurrentSystemStates = Global.CmDataProvider.Value.CMSystemStates.Value.GetAll_ForSystem(cmFeatureTemplate.CMSystemId).ToList();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadComboBoxClasses();

            txtFeatureTemplateName.Text = cmFeatureTemplate.Name;

            dataGridStateTransitionRules.AutoGenerateColumns = false;
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMFeatureStateTransitionRuleDto.Id),
                    Binding = new Binding(nameof(CMFeatureStateTransitionRuleDto.Id)),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMFeatureStateTransitionRuleDto.CMFeatureTemplateId),
                    Binding = new Binding(nameof(CMFeatureStateTransitionRuleDto.CMFeatureTemplateId)),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
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
                    ItemsSource = ConditionAllAnyChoices,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ConditionAllTasks)),

                    // Instructions on how to interact with the "lookup" list
                    SelectedValuePath = nameof(BoolBasedComboBoxEntry.Value),
                    DisplayMemberPath = nameof(BoolBasedComboBoxEntry.Name),
                    Width = 150
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "In state",
                    ItemsSource = CurrentSystemStates,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ConditionQuerySystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                    Width = 200
                });

            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "Are ...",
                    ItemsSource = ConditionAreCompleteChoices,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ConditionTaskComplete)),

                    // Instructions on how to interact with the "lookup" list
                    SelectedValuePath = nameof(BoolBasedComboBoxEntry.Value),
                    DisplayMemberPath = nameof(BoolBasedComboBoxEntry.Name),
                    Width = 150
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridComboBoxColumn()
                {
                    Header = "Then move to state",
                    ItemsSource = CurrentSystemStates,

                    // Where to store the selected value
                    SelectedValueBinding = new Binding(nameof(CMFeatureStateTransitionRuleDto.ToCMSystemStateId)),

                    // Instructions on how to interact with the "lookup" list
                    SelectedValuePath = nameof(CMSystemStateDto.Id),
                    DisplayMemberPath = nameof(CMSystemStateDto.Name),
                    Width = 200
                });

            // Load all state transition rules
            var cmFeatureStateTransitionRules = Global.CmDataProvider.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(cmFeatureTemplate.Id).ToList();
            dataGridStateTransitionRules.ItemsSource = cmFeatureStateTransitionRules;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // Update the feature template. Load it first from the db first just in case it has been updated elsewhere.
            var cmFeatureTemplateDb = Global.CmDataProvider.Value.CMFeatureTemplates.Value.Get(cmFeatureTemplate.Id);
            cmFeatureTemplateDb.Name = txtFeatureTemplateName.Text;
            Global.CmDataProvider.Value.CMFeatureTemplates.Value.Upsert(cmFeatureTemplateDb);

            // Update the collection of transition rules to be what is currently displayed in the data grid
            List<CMFeatureStateTransitionRuleDto> stateTransitionRules = (List<CMFeatureStateTransitionRuleDto>)dataGridStateTransitionRules.ItemsSource;

            // First nuke all existing transition rules.
            // Nothing references these directly, and won't AFAIK so this should be ok.
            Global.CmDataProvider.Value.CMFeatureStateTransitionRules.Value.DeleteAll_ForFeatureTemplate(cmFeatureTemplate.Id);

            // Add the new ones now shown in the grid
            foreach (var rule in stateTransitionRules)
            {
                rule.CMFeatureTemplateId = cmFeatureTemplate.Id; // Make sure the rules are connected to the correct feature template
                Global.CmDataProvider.Value.CMFeatureStateTransitionRules.Value.Upsert(rule);
            }

            MessageBox.Show("Updated");

            // Reload main treeview, this is how we handle renames
            ConfigWindow.ReLoadTreeConfiguration();
        }
    }
}
