using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberMigrate.Extensions
{
    public static class TreeViewExtensions
    {
        /// <summary>
        /// Gets the currently selected TreeView tag item.
        /// </summary>
        /// <returns></returns>
        public static TreeViewTag GetSelectedTreeViewTag(this TreeView treeview)
        {
            var selectedNode = treeview.SelectedItem;
            if (selectedNode == null || !(selectedNode is TreeViewItem))
            {
                return default(TreeViewTag);
            }

            var selectedTreeViewItem = (selectedNode as TreeViewItem);
            if (selectedTreeViewItem?.Tag == null)
            {
                return default(TreeViewTag);
            }

            return selectedTreeViewItem.Tag as TreeViewTag;
        }

        /// <summary>
        /// Due to the fact that the remove function must be called on the parent that owns the item
        /// there are a few possibilities to consider. This function encapsulates that checking logic
        /// so that you can just easily remove whatever the currenly selected item is.
        /// </summary>
        public static void RemoveSelectedNode(this TreeView treeView)
        {
            var selectedItem = treeView.SelectedItem as TreeViewItem;

            // if the parent is null, try to remove it straight from the treeview
            if (selectedItem.Parent == null || !(selectedItem.Parent is TreeViewItem))
            {
                // even this call will silently fail if it doesn't work
                treeView.Items.Remove(selectedItem);
                return;
            }

            // Otherwise find the parent node and execute the remove from from that.
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
        /// Select the nearest treeview element when right clicking if clicking in the treeview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void TreeView_PreviewMouseRightButtonDown_SelectNode(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
        private static TreeViewItem VisualTreeViewItemFinder(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeViewItem;
        }
    }
}
