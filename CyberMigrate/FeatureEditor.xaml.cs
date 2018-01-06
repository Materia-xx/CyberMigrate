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

        public FeatureEditor(CMFeatureDto cmFeatureDto)
        {
            this.cmFeatureDto = cmFeatureDto;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = cmFeatureDto.Name;

            var featureUC = new FeatureEditorUC(cmFeatureDto);
            editorUIPanel.Children.Add(featureUC);
        }
    }
}
