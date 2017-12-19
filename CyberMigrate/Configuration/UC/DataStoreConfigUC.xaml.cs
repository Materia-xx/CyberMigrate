using Dto;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for DataStoreConfigUC.xaml
    /// </summary>
    public partial class DataStoreConfigUC : UserControl
    {
        public DataStoreConfigUC()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var options = Global.CmMasterDataProvider.Value.GetOptions();
            txtStorePath.Text = options.DataStorePath;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStorePath.Text) || !Directory.Exists(txtStorePath.Text))
            {
                MessageBox.Show("Store path must exist.");
                return;
            }

            var options = Global.CmMasterDataProvider.Value.GetOptions();
            options.DataStorePath = txtStorePath.Text;

            Global.CmMasterDataProvider.Value.updateOptions(options);

            MessageBox.Show("Updated.");
        }
    }
}
