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
        public int CMFeatureTemplateId { get; set; }

        public Config ConfigWindow { get; set; }

        public FeatureTemplateConfigUC()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var cmFeatureTemplate = Global.CmDataProvider.Value.CMFeatureTemplates.Value.Get(CMFeatureTemplateId);
            txtFeatureTemplateName.Text = cmFeatureTemplate.Name;

            dataGridStateTransitionRules.AutoGenerateColumns = false;
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Id",
                    Binding = new Binding("Id"),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "If (all / any) tasks",
                    Binding = new Binding("ConditionAllTasks"),
                    Width = 150
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "In state",
                    Binding = new Binding("ConditionQuerySystemStateId"),
                    Width = 100
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Are",  // Are complete, or Are not complete
                    Binding = new Binding("ConditionTaskComplete"),
                    Width = 100
                });
            dataGridStateTransitionRules.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Then move to state",
                    Binding = new Binding("ToCMSystemStateId"),
                    Width = 150
                });

            // Load all state transition rules
            var cmFeatureStateTransitionRules = Global.CmDataProvider.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(CMFeatureTemplateId).ToList();
            dataGridStateTransitionRules.ItemsSource = cmFeatureStateTransitionRules;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // Update the feature template
            var cmFeatureTemplate = Global.CmDataProvider.Value.CMFeatureTemplates.Value.Get(CMFeatureTemplateId);
            cmFeatureTemplate.Name = txtFeatureTemplateName.Text;
            Global.CmDataProvider.Value.CMFeatureTemplates.Value.Upsert(cmFeatureTemplate);

            // Reload main treeview, this is how we handle renames
            ConfigWindow.ReLoadTreeConfiguration();
        }
    }
}
