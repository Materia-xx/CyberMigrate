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
    /// Interaction logic for Task.xaml
    /// </summary>
    public partial class TaskEditor : Window
    {
        private CMTaskDto cmTaskDto;

        public TaskEditor(CMTaskDto cmTaskDto)
        {
            this.cmTaskDto = cmTaskDto;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = cmTaskDto.Title;
        }
    }
}
