using DataProvider;
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
        public Config ConfigWindow { get; set; }

        public CMSystemDto cmSystem;

        public SystemConfigUC(Config configWindow, CMSystemDto cmSystem)
        {
            InitializeComponent();
            this.cmSystem = cmSystem;
            this.ConfigWindow = configWindow;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtSystemName.Text = cmSystem.Name;

            // Don't let the grid auto-generate the columns. Because we want to instead have some of them hidden
            dataGridStates.AutoGenerateColumns = false;
            dataGridStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMSystemStateDto.Id),
                    Binding = new Binding(nameof(CMSystemStateDto.Id)),
                    Visibility = Visibility.Collapsed // Only meant to keep track of ids.
                });
            dataGridStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMSystemStateDto.Priority),
                    Binding = new Binding(nameof(CMSystemStateDto.Priority))
                });
            dataGridStates.Columns.Add(
                new DataGridTextColumn()
                {
                    Header = nameof(CMSystemStateDto.Name),
                    Binding = new Binding(nameof(CMSystemStateDto.Name)),
                    Width = 200
                });

            // Load all states in this system
            var cmSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystem.Id).ToList();
            dataGridStates.ItemsSource = cmSystemStates;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // Update the system name. Load it from the db first just in case anything changed elsewhere.
            var cmSystemDb = CMDataProvider.DataStore.Value.CMSystems.Value.Get(cmSystem.Id);
            cmSystem.Name = txtSystemName.Text;
            CMDataProvider.DataStore.Value.CMSystems.Value.Upsert(cmSystem);

            // Update the collection of system states
            List<CMSystemStateDto> cmSystemStates = (List<CMSystemStateDto>)dataGridStates.ItemsSource;

            // Verify data before doing anything
            foreach (var cmSystemState in cmSystemStates)
            {
                if (string.IsNullOrWhiteSpace(cmSystemState.Name))
                {
                    MessageBox.Show($"A row was found with an empty state name. This is required. Nothing has been updated.");
                    return;
                }
            }

            foreach (var cmSystemState in cmSystemStates)
            {
                // Existing states will already have the right system id, new ones won't. Just set them all.
                cmSystemState.CMSystemId = cmSystemDb.Id;

                // If the Id column is 0 then check to see if it was just removed and added with the same name
                // instead of making it a new state.
                if (cmSystemState.Id == 0)
                {
                    var existingState = CMDataProvider.DataStore.Value.CMSystemStates.Value.Get_ForStateName(cmSystemState.Name, cmSystemDb.Id);
                    if (existingState != null)
                    {
                        cmSystemState.Id = existingState.Id;
                        MessageBox.Show($"State '{cmSystemState.Name}' was re-assigned back to the original state with this name even though the row was removed and re-added. To truly detach a state, do the removal first (then apply), then re-add (then apply again).");
                    }
                }

                // Add a new state or allow renaming or changing of the priority
                CMDataProvider.DataStore.Value.CMSystemStates.Value.Upsert(cmSystemState);
            }

            // Look for states that exist in the database, but not in the grid and delete them if possible
            var cmSystemDBStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystemDb.Id).ToList();
            foreach (var cmSystemState in cmSystemDBStates)
            {
                var gridSystemState = cmSystemStates.FirstOrDefault(s => s.Id == cmSystemState.Id);
                if (gridSystemState == null)
                {
                    // mcbtodo: For now I am just deleting the state, but really it should check to make sure there are no refs to the state first.
                    CMDataProvider.DataStore.Value.CMSystemStates.Value.Delete(cmSystemState.Id);
                }
            }

            // Reload main treeview, this is how we handle renames
            ConfigWindow.ReLoadTreeConfiguration();
        }
    }
}
