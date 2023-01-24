using DataProvider.ProgramConfig;
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
        private Config ConfigWindow { get; set; }

        public CMDataStoreDto cmDataStore;

        public DataStoreConfigUC(Config configWindow, CMDataStoreDto cmDataStore)
        {
            this.cmDataStore = cmDataStore;
            this.ConfigWindow = configWindow;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var options = CMProgramConfig.ReadLocalAppData();
            txtStorePath.Text = options?.DataStorePath;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStorePath.Text) || !Directory.Exists(txtStorePath.Text))
            {
                MessageBox.Show("Store path must exist.");
                return;
            }

            var options = new CMOptionsDto()
            {
                DataStorePath = txtStorePath.Text
            };

            CMProgramConfig.WriteLocalAppData(options);
        }
    }
}
