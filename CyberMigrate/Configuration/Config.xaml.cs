using CyberMigrate.ConfigurationUC;
using DataProvider;
using Dto;
using System;
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

        // special case for task factories to keep track of which tree view node is showing the UI for each task factory
        private class CMTaskFactoryDto : IdBasedObject // mcbtodo: put this in the dto class anyway ?
        {
            public string TaskFactoryName { get; set; }
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
            var dataStoreTreeViewItem = TreeConfiguration_AddDataStore();

            // If the data store path hasn't been set yet, then this is as far as we can go
            if (string.IsNullOrWhiteSpace(CMDataProvider.Master.Value.GetOptions().DataStorePath))
            {
                return;
            }

            // Get all of the systems and show them as tree view items
            var cmSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();
            foreach (var cmSystem in cmSystems)
            {
                var cmSystemTreeViewItem = TreeConfiguration_AddCMSystem(dataStoreTreeViewItem, cmSystem);

                // Get all of the feature templates within this system and show them.
                var cmFeatureTemplates = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_ForSystem(cmSystem.Id, true);
                foreach (var cmFeatureTemplate in cmFeatureTemplates)
                {
                    var cmFeatureTemplateTreeViewItem = TreeConfiguration_AddFeatureTemplate(cmSystemTreeViewItem, cmFeatureTemplate);

                    // Get the possible states for this feature template and show them
                    var featureTemplateStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(cmFeatureTemplate.Id);

                    // mcbtodo: show them as tree view items. This will provide the structure to hang tasks that are templates off of within each state
                }
            }

            dataStoreTreeViewItem.ExpandSubtree();


            // Add the task factories node and UIs to configure each factory
            var taskFactoriesTreeViewItem =  TreeConfiguration_AddTaskFactories();

            foreach (var taskFactory in TaskFactoriesCatalog.Instance.TaskFactories)
            {
                var cmTaskFactoryDto = new CMTaskFactoryDto()
                {
                    TaskFactoryName = taskFactory.GetType().Name
                };

                var taskFactoryTreeViewItem = TreeConfiguration_AddTaskFactory(taskFactoriesTreeViewItem, cmTaskFactoryDto);
            }

            taskFactoriesTreeViewItem.ExpandSubtree();
        }

        private TreeViewItem TreeConfiguration_AddTaskFactory(TreeViewItem taskFactoriesTreeViewItem, CMTaskFactoryDto cmTaskFactoryDto)
        {
            var taskFactoryTreeViewItem = new TreeViewItem()
            {
                Header = cmTaskFactoryDto.TaskFactoryName,
                Tag = new ConfigTreeViewTag(cmTaskFactoryDto),
            };
            taskFactoriesTreeViewItem.Items.Add(taskFactoryTreeViewItem);

            // Add the context menu
            taskFactoryTreeViewItem.ContextMenu = new ContextMenu(); // There is no context menu actions currently for task factories

            taskFactoryTreeViewItem.Selected += TreeConfiguration_NodeSelected;

            return taskFactoryTreeViewItem;
        }

        private TreeViewItem TreeConfiguration_AddTaskFactories()
        {
            var taskFactoriesTreeViewItem = new TreeViewItem()
            {
                Header = "Task Factories",
                Tag = null, // There is no tag here because there is no need to show any UI at this level.
            };
            treeConfig.Items.Add(taskFactoriesTreeViewItem);

            // Add the context menu
            taskFactoriesTreeViewItem.ContextMenu = new ContextMenu(); // There is no context menu actions currently for task factories

            taskFactoriesTreeViewItem.Selected += TreeConfiguration_NodeSelected; // Still keep the onSelected event so the UI can clear what may be there when selected

            return taskFactoriesTreeViewItem;
        }

        private TreeViewItem TreeConfiguration_AddDataStore()
        {
            // Data Store
            var dataStoreTreeViewItem = new TreeViewItem()
            {
                Header = "Data Store",
                Tag = new ConfigTreeViewTag(new CMDataStoreDto()), // mcbtodo: there isn't currently an instance of cmDataStore availalble for this. Instead expose a Get function somewhere to get it.
            };
            treeConfig.Items.Add(dataStoreTreeViewItem);

            // Add the context menu
            dataStoreTreeViewItem.ContextMenu = GetContextMenu_DataStore(dataStoreTreeViewItem);

            dataStoreTreeViewItem.Selected += TreeConfiguration_NodeSelected;

            return dataStoreTreeViewItem;
        }

        private ContextMenu GetContextMenu_DataStore(TreeViewItem dataStoreTreeViewItem)
        {
            // Data Store context menu
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

                if (CMDataProvider.DataStore.Value.CMSystems.Value.Get_ForSystemName(newCMSystem.Name) != null)
                {
                    MessageBox.Show($"A '{newCMSystem.Name}' already exists. Rename that one first.");
                    return;
                }

                if (CMDataProvider.DataStore.Value.CMSystems.Value.Upsert(newCMSystem))
                {
                    TreeConfiguration_AddCMSystem(dataStoreTreeViewItem, newCMSystem);
                }
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

                var selectedCMSystemDto = selectedTreeViewTag.Dto as CMSystemDto;

                // mcbtodo: Check for any refs before deleting, do this as a callback ? e.g. how to provide a delete function that comes with the CRUD object but 
                // mcbtodo: allow for extensibility to allow the creator to provide custom logic to indicate if an id is still referenced.
                // mcbtodo: is there a concept of referential integrity in liteDb ?
                CMDataProvider.DataStore.Value.CMSystems.Value.Delete(selectedCMSystemDto.Id);
                RemoveSelectedTreeConfigItem();
            };
            return contextMenu;
        }

        private TreeViewItem TreeConfiguration_AddCMSystem(TreeViewItem parentTreeViewItem, CMSystemDto cmSystem)
        {
            var cmSystemTreeViewItem = new TreeViewItem()
            {
                Header = cmSystem.Name,
                Tag = new ConfigTreeViewTag(cmSystem)
            };

            parentTreeViewItem.Items.Add(cmSystemTreeViewItem);

            cmSystemTreeViewItem.ContextMenu = GetContextMenu_CMSystem(cmSystemTreeViewItem, cmSystem);

            return cmSystemTreeViewItem;
        }

        private ContextMenu GetContextMenu_CMSystem(TreeViewItem cmSystemTreeViewItem, CMSystemDto cmSystem)
        {
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

                if (CMDataProvider.DataStore.Value.CMFeatures.Value.Get_ForName(newCMFeatureTemplate.Name, cmSystem.Id, true) != null)
                {
                    MessageBox.Show($"A template named '{newCMFeatureTemplate.Name}' already exists. Rename that one first.");
                    return;
                }

                if (CMDataProvider.DataStore.Value.CMFeatures.Value.Upsert(newCMFeatureTemplate))
                {
                    TreeConfiguration_AddFeatureTemplate(cmSystemTreeViewItem, newCMFeatureTemplate);
                }
            };

            return contextMenu;
        }

        private TreeViewItem TreeConfiguration_AddFeatureTemplate(TreeViewItem parentTreeViewItem, CMFeatureDto cmFeatureTemplate)
        {
            var cmFeatureTemplateTreeViewItem = new TreeViewItem()
            {
                Header = cmFeatureTemplate.Name,
                Tag = new ConfigTreeViewTag(cmFeatureTemplate)
            };

            parentTreeViewItem.Items.Add(cmFeatureTemplateTreeViewItem);

            cmFeatureTemplateTreeViewItem.ContextMenu = GetContextMenu_CMFeatureTemplate();

            return cmFeatureTemplateTreeViewItem;
        }

        private ContextMenu GetContextMenu_CMFeatureTemplate()
        {
            var contextMenu = new ContextMenu();
            return contextMenu;
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
                case nameof(CMTaskFactoryDto):
                    var dto = attachedTag.Dto as CMTaskFactoryDto;
                    var taskFactoryUc = TaskFactoriesCatalog.Instance.GetConfigUI(dto.TaskFactoryName);
                    configUIPanel.Children.Add(taskFactoryUc);
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
