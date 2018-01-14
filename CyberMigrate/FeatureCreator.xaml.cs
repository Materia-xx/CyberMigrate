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
using TaskBase.Extensions;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for FeatureCreator.xaml
    /// </summary>
    public partial class FeatureCreator : Window
    {
        public CMFeatureDto CreatedFeature { get; set;  }

        private CMFeatureDto FeatureTemplate { get; set;  }
        public FeatureCreator(CMFeatureDto featureTemplate)
        {
            InitializeComponent();
            this.FeatureTemplate = featureTemplate;

            txtFeatureName.Text = featureTemplate.Name;
        }

        private void btnCreateFeature_Click(object sender, RoutedEventArgs e)
        {
            // Setting the name and color are basically just changing the defaults temporarily in the template before cloning
            FeatureTemplate.Name = txtFeatureName.Text;
            FeatureTemplate.TasksBackgroundColor = cpColor.SelectedColor.ToString();
            CreatedFeature = FeatureTemplate.ToInstance(new List<CMFeatureVarStringDto>());

            this.Close();
        }
    }
}
