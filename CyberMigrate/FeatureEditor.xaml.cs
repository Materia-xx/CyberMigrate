using Dto;
using System.Windows;

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

            var featureUC = new FeatureEditorUC(cmFeatureDto, this);
            editorUIPanel.Children.Add(featureUC);
        }
    }
}
