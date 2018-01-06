using DataProvider;
using Dto;
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
using System.Windows.Shapes;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for FeatureEditor.xaml
    /// </summary>
    public partial class FeatureEditor : Window
    {
        private CMFeatureDto cmFeatureDto;

        public FeatureEditor(CMFeatureDto cmFeatureDto)
        {
            this.cmFeatureDto = cmFeatureDto;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = cmFeatureDto.Name;
            txtFeatureName.Text = cmFeatureDto.Name;
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
