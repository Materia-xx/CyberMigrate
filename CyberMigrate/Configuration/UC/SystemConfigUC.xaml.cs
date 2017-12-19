using Dto;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CyberMigrate.ConfigurationUC
{
    /// <summary>
    /// Interaction logic for SystemConfiguration.xaml
    /// </summary>
    public partial class SystemConfigUC : UserControl
    {
        public int CMSystemId { get; set; }
        public Config ConfigWindow { get; set; }

        public SystemConfigUC()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var cmSystem = Global.CmDataProvider.Instance.CMSystems.Instance.Get(CMSystemId);
            txtSystemName.Text = cmSystem.Name;

            // Don't let the grid auto-generate the columns. Because we want to instead have some of them hidden
            dataGridStates.AutoGenerateColumns = false;
            dataGridStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Id",
                    Binding = new Binding("Id"),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
            dataGridStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Priority",
                    Binding = new Binding("Priority")
                });
            dataGridStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = "Name",
                    Binding = new Binding("Name"),
                    Width = 200
                });

            // Load all states in this system
            var cmSystemStates = Global.CmDataProvider.Instance.CMSystemStates.Instance.GetAll_ForSystem(CMSystemId).ToList();
            dataGridStates.ItemsSource = cmSystemStates;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // Update the system name
            var cmSystem = Global.CmDataProvider.Instance.CMSystems.Instance.Get(CMSystemId);
            cmSystem.Name = txtSystemName.Text;
            Global.CmDataProvider.Instance.CMSystems.Instance.Upsert(cmSystem);

            // Update the collection of system states
            List<CMSystemState> cmSystemStates = (List<CMSystemState>)dataGridStates.ItemsSource;

            // Verify data before doing anything
            foreach (var cmSystemState in cmSystemStates)
            {
                if (string.IsNullOrWhiteSpace(cmSystemState.Name))
                {
                    MessageBox.Show($"A row was found with an empty state name. This is required. Nothing has been updated.");
                    return;
                }

                // Existing states will already have the right system id, new ones won't. Just set them all.
                cmSystemState.CMSystemId = CMSystemId;

                // If the Id column is 0 then check to see if it was just removed and added with the same name
                // instead of making it a new state.
                if (cmSystemState.Id == 0)
                {
                    var existingState = Global.CmDataProvider.Instance.CMSystemStates.Instance.Get_ForStateName(cmSystemState.Name);
                    if (existingState != null)
                    {
                        cmSystemState.Id = existingState.Id;
                        MessageBox.Show($"State '{cmSystemState.Name}' was re-assigned back to the original state with this name even though the row was removed and re-added. To truly detach a state, do the removal first (then apply), then re-add (then apply again).");
                    }
                }

                // Add a new state or allow renaming or changing of the priority
                Global.CmDataProvider.Instance.CMSystemStates.Instance.Upsert(cmSystemState);
            }

// mcbtodo: is this a duplicate loop ?
            foreach (var cmSystemState in cmSystemStates)
            {
                // Existing states will already have the right system id, new ones won't. Just set them all.
                cmSystemState.CMSystemId = CMSystemId;

                // If the Id column is 0 then check to see if it was just removed and added with the same name
                // instead of making it a new state.
                if (cmSystemState.Id == 0)
                {
                    var existingState = Global.CmDataProvider.Instance.CMSystemStates.Instance.Get_ForStateName(cmSystemState.Name);
                    if (existingState != null)
                    {
                        cmSystemState.Id = existingState.Id;
                        MessageBox.Show($"State '{cmSystemState.Name}' was re-assigned back to the original state with this name even though the row was removed and re-added. To truly detach a state, do the removal first (then apply), then re-add (then apply again).");
                    }
                }

                // Add a new state or allow renaming or changing of the priority
                Global.CmDataProvider.Instance.CMSystemStates.Instance.Upsert(cmSystemState);
            }

            // Look for states that exist in the database, but not in the grid and delete them if possible
            var cmSystemDBStates = Global.CmDataProvider.Instance.CMSystemStates.Instance.GetAll_ForSystem(CMSystemId).ToList();
            foreach (var cmSystemState in cmSystemDBStates)
            {
                var gridSystemState = cmSystemStates.FirstOrDefault(s => s.Id == cmSystemState.Id);
                if (gridSystemState == null)
                {
                    // mcbtodo: For now I am just deleting the state, but really it should check to make sure there are no refs to the state first.
                    Global.CmDataProvider.Instance.CMSystemStates.Instance.Delete(cmSystemState.Id);
                }
            }

            // Reload main treeview, this is how we handle renames
            ConfigWindow.ReLoadTreeConfiguration();
        }
    }
}
