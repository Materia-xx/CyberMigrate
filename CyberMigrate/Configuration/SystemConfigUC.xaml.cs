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
    /// Interaction logic for SystemConfiguration.xaml
    /// </summary>
    public partial class SystemConfigUC : UserControl
    {
        public Config ConfigWindow { get; set; }

        public CMSystemDto cmSystem;

        private ObservableCollection<CMSystemStateDto> systemStates = new ObservableCollection<CMSystemStateDto>();

        public SystemConfigUC(Config configWindow, CMSystemDto cmSystem)
        {
            InitializeComponent();
            this.cmSystem = cmSystem;
            this.ConfigWindow = configWindow;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtSystemName.Text = cmSystem.Name;
            Init_StatesGrid();
        }

        private void Init_StatesGrid()
        {
            // Don't let the grid auto-generate the columns. Because we want to instead have some of them hidden
            dataGridStates.AutoGenerateColumns = false;
            dataGridStates.Columns.Clear();

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
            dataGridStates.ItemsSource = systemStates;
            Reload_SystemStates();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridStates.RowEditEnding += DataGridState_RowEditEnding;
        }

        private void Reload_SystemStates()
        {
            systemStates.CollectionChanged -= States_CollectionChanged;
            systemStates.Clear();
            var cmSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystem.Id).ToList();
            foreach (var systemState in cmSystemStates)
            {
                systemStates.Add(systemState);
            }
            systemStates.CollectionChanged += States_CollectionChanged;
        }

        private void States_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removedState in e.OldItems)
                {
                    var gridState = (CMSystemStateDto)removedState;

                    var opResult = CMDataProvider.DataStore.Value.CMSystemStates.Value.Delete(gridState.Id);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Reload the states grid to represent that the item wasn't actually deleted
                        Reload_SystemStates();
                        return;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // The order of operations (I believe) is:
                //  * A new row is added to the datagrid
                //  * A new CMSystemStateDto is constructed and added to the observable collection.
                //    Note that at this point this Dto *may* not be in a valid state to be entered into the db and an insert operation will fail.
                // Therefore we do not do the insert attempt at this point. Instead it is handled in the row update code.
                // However we do set defaults for things here that won't be available to set through the grid UI
                foreach (var addedState in e.NewItems)
                {
                    var gridState = (CMSystemStateDto)addedState;
                    gridState.CMSystemId = cmSystem.Id;
                }
            }
        }

        private void DataGridState_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Unfortunately, I'm unable to find any reference to the "new values" as a Dto object.
                // I imagine it would be possible to dig through the cells and construct it, but IMO that is even worse than this:
                dataGridStates.RowEditEnding -= DataGridState_RowEditEnding;
                dataGridStates.CommitEdit();
                dataGridStates.Items.Refresh();
                dataGridStates.RowEditEnding += DataGridState_RowEditEnding;

                var gridState = (CMSystemStateDto)dataGridStates.SelectedItem;

                if (gridState.Id > 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMSystemStates.Value.Update(gridState);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Since the row has already been commited to the grid above, our only recourse at this point to roll it back is to reload the rules grid
                        Reload_SystemStates();
                        return;
                    }
                }
                else
                {
                    var opResult = CMDataProvider.DataStore.Value.CMSystemStates.Value.Insert(gridState);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);

                        // Keep the row around so the user has a chance to correct it.
                        return;
                    }
                }
            }
        }

        private void txtSystemName_LostFocus(object sender, RoutedEventArgs e)
        {
            var opResult = CMDataProvider.DataStore.Value.CMSystems.Value.UpdateIfNeeded_Name(cmSystem.Id, txtSystemName.Text);
            if (opResult.Errors.Any())
            {
                MessageBox.Show(opResult.ErrorsCombined);
                txtSystemName.Text = cmSystem.Name;
                return;
            }

            cmSystem = CMDataProvider.DataStore.Value.CMSystems.Value.Get(cmSystem.Id);
        }
    }
}
