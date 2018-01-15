using DataProvider;
using System.Windows;

namespace CyberMigrate.Extensions
{
    public static class WindowExtensions
    {
        public static void SaveWindowDimensions(this Window window, string dimensionName)
        {
            CMDataProvider.DataStore.Value.CMDimensionInfos.Value.SaveDimensions(dimensionName, 
                window.Top,
                window.Left,
                window.Width,
                window.Height);
        }

        public static void LoadWindowDimensions(this Window window, string dimensionName)
        {
            var windowDimensions = CMDataProvider.DataStore.Value.CMDimensionInfos.Value.Get_ForName(dimensionName);
            if (windowDimensions != null)
            {
                window.Top = windowDimensions.Top;
                window.Left = windowDimensions.Left;
                window.Width = windowDimensions.Width;
                window.Height = windowDimensions.Height;
            }
        }
    }
}
