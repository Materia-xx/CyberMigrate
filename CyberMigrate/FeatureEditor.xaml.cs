using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using System.Windows.Shapes;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for FeatureEditor.xaml
    /// </summary>
    public partial class FeatureEditor : Window
    {
        private CMFeatureDto cmFeatureDto;

        private ObservableCollection<CMFeatureVarStringDto> featureVariables = new ObservableCollection<CMFeatureVarStringDto>();

        public FeatureEditor(CMFeatureDto cmFeatureDto)
        {
            this.cmFeatureDto = cmFeatureDto;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = cmFeatureDto.Name;
            txtFeatureName.Text = cmFeatureDto.Name;
            Init_FeatureVarsGrid();
        }

        private void Init_FeatureVarsGrid()
        {
            dataGridFeatureVars.ItemsSource = featureVariables;
            Reload_FeatureVariables();

            // The way I've implemented it, this observable collection doesn't have detection if a property is updated, so we do that here
            dataGridFeatureVars.RowEditEnding += DataGridFeatureVariables_RowEditEnding;
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
        }
    }
}
