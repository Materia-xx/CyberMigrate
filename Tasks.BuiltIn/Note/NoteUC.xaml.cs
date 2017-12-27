using DataProvider;
using Dto;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Tasks.BuiltIn.Note
{
    /// <summary>
    /// Interaction logic for NoteUC.xaml
    /// </summary>
    public partial class NoteUC : UserControl
    {
        private CMSystemDto cmSystem;
        private CMFeatureDto cmFeature;
        private CMTaskDto cmTask;

        private NoteDto TaskData;

        public NoteUC(CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            InitializeComponent();

            this.cmSystem = cmSystem;
            this.cmFeature = cmFeature;
            this.cmTask = cmTask;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            TaskData = BuildInTasksDataProviders.NoteDataProvider.Get_ForTaskId(cmTask.Id);

            if (TaskData != null)
            {
                try
                {
                    txtNote.Text = TaskData.Note;
                }
                catch
                {
                    MessageBox.Show("Error reading task data.");
                }
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (TaskData == null)
            {
                TaskData = new NoteDto()
                {
                    Note = txtNote.Text
                };
            }

            CMCUDResult opResult;
            if (TaskData.Id == 0)
            {
                opResult = BuildInTasksDataProviders.NoteDataProvider.Insert(TaskData);
            }
            else
            {
                opResult = BuildInTasksDataProviders.NoteDataProvider.Update(TaskData);
            }

            if (opResult.Errors.Any())
            {
                MessageBox.Show(opResult.ErrorsCombined);
            }
            else
            {
                MessageBox.Show("Updated");
            }
        }
    }
}
