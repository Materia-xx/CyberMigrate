using CyberMigrate.Configuration;
using CyberMigrate.Extensions;
using DataProvider;
using Dto;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskBase;

namespace CyberMigrate
{
    /// <summary>
    /// Interaction logic for Config.xaml
    /// </summary>
    public partial class Config : Window
    {
        private MainWindow MainForm { get; set; }

        public Config(MainWindow mainForm)
        {
            this.MainForm = mainForm;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Select the node that is hovered over when right clicking and before showing the context menu
            treeConfig.PreviewMouseRightButtonDown += TreeViewExtensions.TreeView_PreviewMouseRightButtonDown_SelectNode;
            ReLoadTreeConfiguration();
        }

        public void ReLoadTreeConfiguration()
        {
            treeConfig.Items.Clear();

            var dataStoreTVI = GetTVI_DataStore();
            treeConfig.Items.Add(dataStoreTVI);

            // If the data store path hasn't been set yet, then this is as far as we can go
            if (string.IsNullOrWhiteSpace(CMDataProvider.Master.Value.GetOptions().DataStorePath))
            {
                return;
            }

            // Get all of the systems and show them as tree view items
            var cmSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();
            foreach (var cmSystem in cmSystems)
            {
                var cmSystemTVI = GetTVI_System(cmSystem);
                dataStoreTVI.Items.Add(cmSystemTVI);

                // Get all of the feature templates within this system and show them.
                var cmFeatureTemplates = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_ForSystem(cmSystem.Id, true);
                foreach (var cmFeatureTemplate in cmFeatureTemplates)
                {
                    var cmFeatureTemplateTVI = GetTVI_FeatureTemplate(cmFeatureTemplate);
                    cmSystemTVI.Items.Add(cmFeatureTemplateTVI);
                }
            }

            dataStoreTVI.ExpandSubtree();

            // Add the task factories node and UIs to configure each factory
            var taskFactoriesTVI =  GetTVI_TaskFactories();
            treeConfig.Items.Add(taskFactoriesTVI);

            foreach (var taskFactory in TaskFactoriesCatalog.Instance.TaskFactories)
            {
                var cmTaskFactoryDto = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Get_ForName(taskFactory.Name);

                var taskFactoryTVI = GetTVI_TaskFactory(cmTaskFactoryDto);
                taskFactoriesTVI.Items.Add(taskFactoryTVI);

                // Add a config UI for each task type within this task factory
                foreach (var taskType in taskFactory.GetTaskTypes())
                {
                    var cmTaskTypeDto = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(taskType);

                    var taskTypeConfigTVI = GetTVI_TaskType(cmTaskTypeDto);
                    taskFactoryTVI.Items.Add(taskTypeConfigTVI);
                }
            }

            taskFactoriesTVI.ExpandSubtree();
        }

        private TreeViewItem GetTVI_TaskFactories()
        {
            var taskFactoriesTreeViewItem = new TreeViewItem()
            {
                Header = "Task Factories",
                Tag = null, // There is no tag here because there is no need to show any UI at this level.
            };

            // Add the context menu
            // There is no context menu actions currently for task factories
            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            taskFactoriesTreeViewItem.ContextMenu = contextMenu; 

            taskFactoriesTreeViewItem.Selected += TreeConfiguration_NodeSelected; // Still keep the onSelected event so the UI can clear what may be there when selected

            return taskFactoriesTreeViewItem;
        }

        private TreeViewItem GetTVI_TaskFactory(CMTaskFactoryDto cmTaskFactoryDto)
        {
            var taskFactoryTVI = new TreeViewItem()
            {
                Header = cmTaskFactoryDto.Name,
                Tag = new TreeViewTag(cmTaskFactoryDto),
            };

            // Add the context menu
            // There is no context menu actions currently for task factories
            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            taskFactoryTVI.ContextMenu = contextMenu;

            taskFactoryTVI.Selected += TreeConfiguration_NodeSelected;

            return taskFactoryTVI;
        }

        private TreeViewItem GetTVI_TaskType(CMTaskTypeDto cmTaskTypeDto)
        {
            var taskTypeTVI = new TreeViewItem()
            {
                Header = cmTaskTypeDto.Name,
                Tag = new TreeViewTag(cmTaskTypeDto),
            };

            // Add the context menu
            // There is no context menu actions currently for task types
            var contextMenu = new ContextMenu();
            contextMenu.Visibility = Visibility.Hidden;
            taskTypeTVI.ContextMenu = contextMenu; 

            taskTypeTVI.Selected += TreeConfiguration_NodeSelected;

            return taskTypeTVI;
        }

        private TreeViewItem GetTVI_DataStore()
        {
            var dataStoreTreeViewItem = new TreeViewItem()
            {
                Header = "Data Store",
                Tag = new TreeViewTag(new CMDataStoreDto()), // mcbtodo: there isn't currently an instance of cmDataStore availalble for this. Instead expose a Get function somewhere to get it.
            };

            var contextMenu = new ContextMenu();

            var addNewSystemMenu = new MenuItem()
            {
                Header = "Add New System"
            };

            contextMenu.Items.Add(addNewSystemMenu);
            addNewSystemMenu.Click += (sender, e) =>
            {
                var newCMSystem = new CMSystemDto()
                {
                    Name = "New System"
                };

                var opResult = CMDataProvider.DataStore.Value.CMSystems.Value.Insert(newCMSystem);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                    return;
                }

                var systemTVI = GetTVI_System(newCMSystem);
                dataStoreTreeViewItem.Items.Add(systemTVI);
            };

            dataStoreTreeViewItem.ContextMenu = contextMenu;

            dataStoreTreeViewItem.Selected += TreeConfiguration_NodeSelected;

            return dataStoreTreeViewItem;
        }

        private TreeViewItem GetTVI_System(CMSystemDto cmSystem)
        {
            var cmSystemTreeViewItem = new TreeViewItem()
            {
                Header = cmSystem.Name,
                Tag = new TreeViewTag(cmSystem)
            };

            var contextMenu = new ContextMenu();

            var addNewFeatureTemplate = new MenuItem()
            {
                Header = "Add New Feature Template"
            };
            contextMenu.Items.Add(addNewFeatureTemplate);
            addNewFeatureTemplate.Click += (sender, e) =>
            {
                var newCMFeatureTemplate = new CMFeatureDto()
                {
                    Name = "New Feature Template",
                    CMSystemId = cmSystem.Id
                };

                var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Insert(newCMFeatureTemplate);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                    return;
                }

                // Add a set of default transition rules that match the current system states
                int rulePriority = 1;
                var systemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForSystem(cmSystem.Id).ToList();
                // These are the rules that detect that an un-closed task exists and to snap back into that state in priority order
                for (int i = systemStates.Count() - 1; i >= 0; i--)
                {
                    var currentState = systemStates[i];

                    var newTransitionRule = new CMFeatureStateTransitionRuleDto()
                    {
                        CMFeatureId = newCMFeatureTemplate.Id,
                        ConditionAllTasks = false,
                        ConditionQuerySystemStateId = currentState.Id,
                        ConditionTaskClosed = false,
                        Priority = rulePriority++,
                        ToCMSystemStateId = currentState.Id
                    };
                    var opResultRule = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Insert(newTransitionRule);
                    if (opResultRule.Errors.Any())
                    {
                        MessageBox.Show(opResultRule.ErrorsCombined);
                        return;
                    }
                }
                // These are the auto-progressing rules, which if the priority wasn't set up or is done so in a specialized way.. they might be
                // wrong for the template that is being created. We'll pop up a message box for the user to validate. I figure it's easier to
                // delete a couple rows and maybe change a couple values than to manually add everything in.
                for (int i = systemStates.Count()-1; i > 0; i--)
                {
                    var currentState = systemStates[i];
                    var nextState = systemStates[i - 1];

                    var newTransitionRule = new CMFeatureStateTransitionRuleDto()
                    {
                        CMFeatureId = newCMFeatureTemplate.Id,
                        ConditionAllTasks = true,
                        ConditionQuerySystemStateId = currentState.Id,
                        ConditionTaskClosed = true,
                        Priority = rulePriority++,
                        ToCMSystemStateId = nextState.Id
                    };
                    var opResultRule = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Insert(newTransitionRule);
                    if (opResultRule.Errors.Any())
                    {
                        MessageBox.Show(opResultRule.ErrorsCombined);
                        return;
                    }
                }

                if (systemStates.Any())
                {
                    MessageBox.Show("Feature state transition rules were automatically added. Please validate these rules.");
                }

                var featureTemplateTVI = GetTVI_FeatureTemplate(newCMFeatureTemplate);
                cmSystemTreeViewItem.Items.Add(featureTemplateTVI);
            };

            var deleteSystemMenu = new MenuItem()
            {
                Header = "Delete System"
            };
            contextMenu.Items.Add(deleteSystemMenu);
            deleteSystemMenu.Click += (sender, e) =>
            {
                var selectedTreeViewTag = treeConfig.GetSelectedTreeViewTag();

                if (selectedTreeViewTag?.Dto == null || !(selectedTreeViewTag?.Dto is CMSystemDto))
                {
                    return;
                }

                // mcbtodo: Add confirm dialogs on delete operations where appropriate
                var selectedCMSystemDto = selectedTreeViewTag.Dto as CMSystemDto;

                var opResult = CMDataProvider.DataStore.Value.CMSystems.Value.Delete(selectedCMSystemDto.Id);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                    return;
                }
                treeConfig.RemoveSelectedNode();
            };

            cmSystemTreeViewItem.ContextMenu = contextMenu;

            return cmSystemTreeViewItem;
        }

        private TreeViewItem GetTVI_FeatureTemplate(CMFeatureDto cmFeatureTemplate)
        {
            var cmFeatureTemplateTreeViewItem = new TreeViewItem()
            {
                Header = cmFeatureTemplate.Name,
                Tag = new TreeViewTag(cmFeatureTemplate)
            };

            var contextMenu = new ContextMenu();

            var deleteFeatureTemplateMenu = new MenuItem()
            {
                Header = "Delete Feature Template"
            };
            contextMenu.Items.Add(deleteFeatureTemplateMenu);
            deleteFeatureTemplateMenu.Click += (sender, e) =>
            {
                var selectedTreeViewTag = treeConfig.GetSelectedTreeViewTag();

                if (selectedTreeViewTag?.Dto == null || !(selectedTreeViewTag?.Dto is CMFeatureDto))
                {
                    return;
                }

                var selectedFeatureTemplateDto = selectedTreeViewTag.Dto as CMFeatureDto;

                var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Delete(selectedFeatureTemplateDto.Id);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                    return;
                }
                treeConfig.RemoveSelectedNode();
            };
            cmFeatureTemplateTreeViewItem.ContextMenu = contextMenu;

            return cmFeatureTemplateTreeViewItem;
        }

        private void TreeConfiguration_NodeSelected(object sender, RoutedEventArgs e)
        {
            configUIPanel.Children.Clear(); // mcbtodo: check to see if there are any unsaved changes before doing this.

            var attachedTag = treeConfig.GetSelectedTreeViewTag();
            if (attachedTag?.Dto == null)
            {
                return;
            }

            switch (attachedTag.DtoTypeName)
            {
                case nameof(CMDataStoreDto):
                    var dataStoreConfigUc = new DataStoreConfigUC(this, MainForm, attachedTag.Dto as CMDataStoreDto);
                    configUIPanel.Children.Add(dataStoreConfigUc);
                    break;
                case nameof(CMSystemDto):
                    var systemConfigUc = new SystemConfigUC(this, attachedTag.Dto as CMSystemDto);
                    configUIPanel.Children.Add(systemConfigUc);
                    break;
                case nameof(CMFeatureDto):
                    var featureTemplateConfigUc = new FeatureTemplateConfigUC(this, attachedTag.Dto as CMFeatureDto);
                    configUIPanel.Children.Add(featureTemplateConfigUc);
                    break;
                case nameof(CMTaskTypeDto):
                    var taskTypeUc = new TaskTypeConfigUC(this, attachedTag.Dto as CMTaskTypeDto);
                    configUIPanel.Children.Add(taskTypeUc);
                    break;
                default:
                    break;
            }
        }
    }
}
