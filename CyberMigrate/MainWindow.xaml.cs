using DataProvider;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
            var options = CMDataProvider.Master.Value.GetOptions();
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
