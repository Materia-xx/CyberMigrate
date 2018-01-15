using DataProvider;
using System.Windows;
using System.Windows.Controls;

namespace CyberMigrate.Extensions
{
    public static class GridExtensions
    {
        public static void SaveGridDimensions(this Grid grid, string dimensionPrefix)
        {
            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                string dimensionName = $"{dimensionPrefix}_Col{i}";
                double gut = ((double)grid.ColumnDefinitions[i].Width.GridUnitType);
                CMDataProvider.DataStore.Value.CMDimensionInfos.Value.SaveDimensions(dimensionName, gut, 0, grid.ColumnDefinitions[i].Width.Value, 0);
            }

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                string dimensionName = $"{dimensionPrefix}_Row{i}";
                double gut = ((double)grid.RowDefinitions[i].Height.GridUnitType);
                CMDataProvider.DataStore.Value.CMDimensionInfos.Value.SaveDimensions(dimensionName, gut, 0, 0, grid.RowDefinitions[i].Height.Value);
            }
        }

        public static void LoadGridDimensions(this Grid grid, string dimensionPrefix)
        {
            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                string dimensionName = $"{dimensionPrefix}_Col{i}";
                var colDimensions = CMDataProvider.DataStore.Value.CMDimensionInfos.Value.Get_ForName(dimensionName);
                if (colDimensions != null)
                {
                    // Top stores the GridUnitType
                    GridUnitType gut = (GridUnitType)colDimensions.Top;
                    grid.ColumnDefinitions[i].Width = new GridLength(colDimensions.Width, gut);
                }
            }

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                string dimensionName = $"{dimensionPrefix}_Row{i}";
                var rowDimensions = CMDataProvider.DataStore.Value.CMDimensionInfos.Value.Get_ForName(dimensionName);
                if (rowDimensions != null)
                {
                    // Top stores the GridUnitType
                    GridUnitType gut = (GridUnitType)rowDimensions.Top;
                    grid.RowDefinitions[i].Height = new GridLength(rowDimensions.Height, gut);
                }
            }
        }
    }
}
