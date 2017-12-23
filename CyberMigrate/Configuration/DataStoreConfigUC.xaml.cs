using DataProvider;
using Dto;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CyberMigrate.Configuration
{
    /// <summary>
    /// Interaction logic for DataStoreConfigUC.xaml
    /// </summary>
    public partial class DataStoreConfigUC : UserControl
    {
        public Config ConfigWindow { get; set; }

        public CMDataStoreDto cmDataStore;

        public DataStoreConfigUC(Config configWindow, CMDataStoreDto cmDataStore)
        {
            InitializeComponent();
            this.cmDataStore = cmDataStore;
            this.ConfigWindow = configWindow;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var options = CMDataProvider.Master.Value.GetOptions();
            txtStorePath.Text = options.DataStorePath;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStorePath.Text) || !Directory.Exists(txtStorePath.Text))
            {
                MessageBox.Show("Store path must exist.");
                return;
            }

            var options = CMDataProvider.Master.Value.GetOptions();
            options.DataStorePath = txtStorePath.Text;

            CMDataProvider.Master.Value.UpdateOptions(options);

            MessageBox.Show("Updated.");

            ConfigWindow.MainForm.DataStorePathSet();
        }
    }
}
