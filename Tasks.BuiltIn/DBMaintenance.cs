using DataProvider;
using Dto;
using System.Linq;
using System.Windows;
using Tasks.BuiltIn.FeatureDependency;

namespace Tasks.BuiltIn
{
    public static class DBMaintenance
    {
        private static int DatabaseTaskLibrarySchemaVersion { get; set; }

        public static bool RunMaintenanceRoutines()
        {
            // Get task factory record for this task factory
            var databasetaskFactoryDto = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Get_ForName(nameof(CMBuiltInTaskFactory));

            // Keep CMBuiltInTaskFactory.Version at the same version that is represented here
            if (databasetaskFactoryDto.Version < 2)
            {
                if (!UpgradeSchemaTo_Version2(databasetaskFactoryDto))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool UpgradeSchemaTo_Version2(CMTaskFactoryDto taskFactoryDto)
        {
            var userResponse = MessageBox.Show($"Upgrading task factory '{nameof(CMBuiltInTaskFactory)}' schema to version 2. Press OK to continue or cancel to quit.", "Upgrade to schema version 2", MessageBoxButton.OKCancel);
            if (userResponse != MessageBoxResult.OK)
            {
                return false;
            }

            // Version 2 update the feature dto to be able to hold choices that lead to no dependent feature intentionally.
            // Upgrade all records that currently exist and are pointing at a feature to the new format.
            var allFeatureDependencyTaskData = FeatureDependencyExtensions.FeatureDependencyDataProvider.GetAll();
            foreach (var featureDependency in allFeatureDependencyTaskData)
            {
                // Delete orphaned feature dependencies
                if (featureDependency.TaskId == 0)
                {
                    var opDelResult = FeatureDependencyExtensions.FeatureDependencyDataProvider.Delete(featureDependency.Id);
                    if (opDelResult.Errors.Any())
                    {
                        MessageBox.Show($"An unrecoverable task factory '{nameof(CMBuiltInTaskFactory)}' database upgrade error has occurred:\r\n{opDelResult.ErrorsCombined}");
                        return false;
                    }
                    continue;
                }

                featureDependency.PathOptionChosen = (featureDependency.InstancedCMFeatureId != 0);
                var opResult = FeatureDependencyExtensions.FeatureDependencyDataProvider.Update(featureDependency);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show($"An unrecoverable task factory '{nameof(CMBuiltInTaskFactory)}' database upgrade error has occurred:\r\n{opResult.ErrorsCombined}");
                    return false;
                }
            }

            // Set the task factory to now be version 2
            taskFactoryDto.Version = 2;
            CMDataProvider.DataStore.Value.CMTaskFactories.Value.Update(taskFactoryDto);
            return true;
        }
    }
}
