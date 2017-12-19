using DataProvider;
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
using TaskBase;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RedrawMainMenu();

            // Automatically show the configuration if the program has not been set up.
            if (!DataStoreOptionConfigured())
            {
                ShowConfiguration();
            }
        }

        private bool DataStoreOptionConfigured()
        {
            var options = Global.CmMasterDataProvider.Instance.GetOptions();
            if (!Directory.Exists(options.DataStorePath))
            {
                return false;
            }
            return true;
        }

        public void ShowConfiguration()
        {
            var optionsWindow = new Config();
            optionsWindow.ShowDialog();
        }

        public void RedrawMainMenu()
        {
            MainMenu.Items.Clear();

            var configurationMenu = new MenuItem() { Header = "Config" };
            configurationMenu.Click += (sender, e) =>
            {
                ShowConfiguration();
            };
            MainMenu.Items.Add(configurationMenu);
        }
    }
}
