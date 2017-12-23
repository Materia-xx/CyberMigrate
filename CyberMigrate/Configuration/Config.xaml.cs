using CyberMigrate.Configuration;
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
        public MainWindow MainForm { get; set; }

        /// <summary>
        /// Attached to nodes in the configuration tree view. Used to store information such as the Dto that each node represents
        /// </summary>
        private class ConfigTreeViewTag
        {
            public ConfigTreeViewTag(IdBasedObject dto)
            {
                this.Dto = dto;
            }

            public IdBasedObject Dto { get; private set; }

            public string DtoTypeName
            {
                get
                {
                    if (Dto == null)
                    {
                        return string.Empty;
                    }
                    return Dto.GetType().Name;
                }
            }
        }

        public Config()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            taskFactoriesTreeViewItem.ContextMenu = new ContextMenu(); // There is no context menu actions currently for task factories

            taskFactoriesTreeViewItem.Selected += TreeConfiguration_NodeSelected; // Still keep the onSelected event so the UI can clear what may be there when selected

            return taskFactoriesTreeViewItem;
        }

        private TreeViewItem GetTVI_TaskFactory(CMTaskFactoryDto cmTaskFactoryDto)
        {
            var taskFactoryTVI = new TreeViewItem()
            {
                Header = cmTaskFactoryDto.Name,
                Tag = new ConfigTreeViewTag(cmTaskFactoryDto),
            };

            // Add the context menu
            taskFactoryTVI.ContextMenu = new ContextMenu(); // There is no context menu actions currently for task factories

            taskFactoryTVI.Selected += TreeConfiguration_NodeSelected;

            return taskFactoryTVI;
        }

        private TreeViewItem GetTVI_TaskType(CMTaskTypeDto cmTaskTypeDto)
        {
            var taskTypeTVI = new TreeViewItem()
            {
                Header = cmTaskTypeDto.Name,
                Tag = new ConfigTreeViewTag(cmTaskTypeDto),
            };

            // Add the context menu
            taskTypeTVI.ContextMenu = new ContextMenu(); // There is no context menu actions currently for task types

            taskTypeTVI.Selected += TreeConfiguration_NodeSelected;

            return taskTypeTVI;
        }

        private TreeViewItem GetTVI_DataStore()
        {
            var dataStoreTreeViewItem = new TreeViewItem()
            {
                Header = "Data Store",
                Tag = new ConfigTreeViewTag(new CMDataStoreDto()), // mcbtodo: there isn't currently an instance of cmDataStore availalble for this. Instead expose a Get function somewhere to get it.
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
                Tag = new ConfigTreeViewTag(cmSystem)
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
                    IsTemplate = true,
                    Name = "New Feature Template",
                    CMSystemId = cmSystem.Id
                };

                var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Insert(newCMFeatureTemplate);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                    return;
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
                var selectedTreeViewTag = GetSelectedConfigTreeViewTag();

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
                RemoveSelectedTreeConfigItem();
            };

            cmSystemTreeViewItem.ContextMenu = contextMenu;

            return cmSystemTreeViewItem;
        }

        private TreeViewItem GetTVI_FeatureTemplate(CMFeatureDto cmFeatureTemplate)
        {
            var cmFeatureTemplateTreeViewItem = new TreeViewItem()
            {
                Header = cmFeatureTemplate.Name,
                Tag = new ConfigTreeViewTag(cmFeatureTemplate)
            };

            cmFeatureTemplateTreeViewItem.ContextMenu = new ContextMenu();

            return cmFeatureTemplateTreeViewItem;
        }

        private void RemoveSelectedTreeConfigItem()
        {
            var selectedItem = treeConfig.SelectedItem as TreeViewItem;
            
            // if the parent is null, try to remove it straight from the treeview
            if (selectedItem.Parent == null || !(selectedItem.Parent is TreeViewItem))
            {
                // even this call will silently fail if it doesn't work
                treeConfig.Items.Remove(selectedItem);
                return;
            }

            // Otherwise find the parent node and execute the from from that.
            // The remove function will only search first level children for the node to delete.
            var parentItem = selectedItem.Parent as TreeViewItem;
            if (parentItem == null)
            {
                // No parent ? How did we get here?
                throw new InvalidOperationException("Unable to find parent node of the node that is being deleted.");
            }

            parentItem.Items.Remove(selectedItem);
        }

        /// <summary>
        /// Gets the currently selected TreeView tag item.
        /// </summary>
        /// <returns></returns>
        private ConfigTreeViewTag GetSelectedConfigTreeViewTag()
        {
            var selectedNode = treeConfig.SelectedItem;
            if (selectedNode == null || !(selectedNode is TreeViewItem))
            {
                return default(ConfigTreeViewTag);
            }

            var selectedTreeViewItem = (selectedNode as TreeViewItem);
            if (selectedTreeViewItem?.Tag == null)
            {
                return default(ConfigTreeViewTag);
            }

            return selectedTreeViewItem.Tag as ConfigTreeViewTag;
        }

        private void TreeConfiguration_NodeSelected(object sender, RoutedEventArgs e)
        {
            configUIPanel.Children.Clear(); // mcbtodo: check to see if there are any unsaved changes before doing this.

            var attachedTag = GetSelectedConfigTreeViewTag();
            if (attachedTag?.Dto == null)
            {
                return;
            }

            switch (attachedTag.DtoTypeName)
            {
                case nameof(CMDataStoreDto):
                    var dataStoreConfigUc = new DataStoreConfigUC(this, attachedTag.Dto as CMDataStoreDto);
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

        /// <summary>
        /// Select the nearest treeview element when right clicking if clicking in the treeview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeConfig_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var treeViewItem = VisualTreeViewItemFinder(e.OriginalSource as DependencyObject);
            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                treeViewItem.IsSelected = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Finds the treeview node closest to where the mouse was clicked and returns it.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private TreeViewItem VisualTreeViewItemFinder(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeViewItem;
        }

    }
}
