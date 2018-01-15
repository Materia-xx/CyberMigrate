using DataProvider;
using System.Windows.Controls;

namespace CyberMigrate.Extensions
{
    public static class DataGridExtensions
    {
        public static void SaveDataGridDimensions(this DataGrid grid, string dimensionPrefix)
        {
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                string dimensionName = $"{dimensionPrefix}_Col{i}";
                double glut = ((double)grid.Columns[i].Width.UnitType);
                CMDataProvider.DataStore.Value.CMDimensionInfos.Value.SaveDimensions(dimensionName, glut, 0, grid.Columns[i].Width.Value, 0);
            }
        }

        public static void LoadDataGridDimensions(this DataGrid grid, string dimensionPrefix)
        {
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                string dimensionName = $"{dimensionPrefix}_Col{i}";
                var colDimensions = CMDataProvider.DataStore.Value.CMDimensionInfos.Value.Get_ForName(dimensionName);
                if (colDimensions != null)
                {
                    // Top stores the DataGridLengthUnitType
                    DataGridLengthUnitType glut = (DataGridLengthUnitType)colDimensions.Top;
                    grid.Columns[i].Width = new DataGridLength(colDimensions.Width, glut);
                }
            }
        }
    }
}
