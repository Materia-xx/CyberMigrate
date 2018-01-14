using DataProvider;
using System.Linq;
using System.Windows;

namespace CyberMigrate
{
    public static class DBMaintenance
    {
        public static bool RunMaintenanceRoutines()
        {
            // Gets the schema version that the database is currently at. If unknown defaults to the first supported version.
            var databaseSchemaVersion = CMDataProvider.DataStore.Value.GetDatabaseSchemaVersion();

            if (databaseSchemaVersion < 2)
            {
                if (!UpgradeSchemaTo_Version2())
                {
                    return false;
                }
            }

            if (databaseSchemaVersion < 3)
            {
                if (!UpgradeSchemaTo_Version3())
                {
                    return false;
                }
            }

            if (databaseSchemaVersion < 4)
            {
                if (!UpgradeSchemaTo_Version4())
                {
                    return false;
                }
            }


            //Upgrade_TaskDto();
            //Upgrade_FeatureDto();
            //Upgrade_TransitionRuleDto();
            //Upgrade_SystemDto();

            // mcbtodo: These are only cleanup routines I'm using to clean up the db during dev and won't be needed in the released version
            //Debug_DeleteTasksNotInAFeature();
            //Debug_DeleteInvalidFeatureTransitionRules();
            //Debug_DeleteAllTaskAndFeatureInstances();
            //Debug_DeleteAllTaskTypesAndStates();
            //Debug_DeleteOptions();

            return true;
        }

        private static bool UpgradeSchemaTo_Version2()
        {
            var userResponse = MessageBox.Show("Upgrading database schema to version 2. Press OK to continue or cancel to quit.", "Upgrade to schema version 2", MessageBoxButton.OKCancel);
            if (userResponse != MessageBoxResult.OK)
            {
                return false;
            }

            // Rewrite all system states to the db so they get the default MigrationOrder property set in the BSON data
            var allSystemStates = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll();
            foreach (var systemState in allSystemStates)
            {
                systemState.MigrationOrder = systemState.Priority;

                var opResult = CMDataProvider.DataStore.Value.CMSystemStates.Value.Update(systemState);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show($"An unrecoverable database upgrade error has occurred:\r\n{opResult.ErrorsCombined}");
                    return false;
                }
            }

            // Set the db to now be version 2
            CMDataProvider.DataStore.Value.SetDatabaseSchemaVersion(2);
            return true;
        }

        private static bool UpgradeSchemaTo_Version3()
        {
            var userResponse = MessageBox.Show("Upgrading database schema to version 3. Press OK to continue or cancel to quit.", "Upgrade to schema version 3", MessageBoxButton.OKCancel);
            if (userResponse != MessageBoxResult.OK)
            {
                return false;
            }

            // Task factory version is a new thing in v3.
            // Rewrite all of the task factory entries to have a default version of 0.
            // Task factories should themselves take care of updating anything appropriate up from 0.
            var allTaskFactories = CMDataProvider.DataStore.Value.CMTaskFactories.Value.GetAll();
            foreach (var taskFactory in allTaskFactories)
            {
                taskFactory.Version = 0;

                var opResult = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Update(taskFactory);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show($"An unrecoverable database upgrade error has occurred:\r\n{opResult.ErrorsCombined}");
                    return false;
                }
            }

            // Set the db to now be version 3
            CMDataProvider.DataStore.Value.SetDatabaseSchemaVersion(3);
            return true;
        }

        private static bool UpgradeSchemaTo_Version4()
        {
            var userResponse = MessageBox.Show("Upgrading database schema to version 4. Press OK to continue or cancel to quit.", "Upgrade to schema version 4", MessageBoxButton.OKCancel);
            if (userResponse != MessageBoxResult.OK)
            {
                return false;
            }

            // v4 - Adds color of task backgrounds to the feature dto

            // Give every feature a default color of an empty string (default grid color)
            var allFeatures = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll();
            foreach (var feature in allFeatures)
            {
                feature.TasksBackgroundColor = null;
                var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Update(feature);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show($"An unrecoverable database upgrade error has occurred:\r\n{opResult.ErrorsCombined}");
                    return false;
                }
            }

            CMDataProvider.DataStore.Value.SetDatabaseSchemaVersion(4);
            return true;
        }

        public static void Debug_DeleteOptions()
        {
            CMDataProvider.Master.Value.DeleteOptions();
        }

        // Remove all task types and states, the program should load new ones from the tasks and defaults. 
        // Also wipes out user defined states if possible
        public static void Debug_DeleteAllTaskTypesAndStates()
        {
            var allTaskTypes = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll();
            foreach (var taskType in allTaskTypes)
            {
                CMDataProvider.DataStore.Value.CMTaskTypes.Value.Delete(taskType.Id);
            }

            var allStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll();
            foreach (var state in allStates)
            {
                CMDataProvider.DataStore.Value.CMTaskStates.Value.Delete(state.Id);
            }
        }

        private static void Upgrade_TransitionRuleDto()
        {
            MessageBox.Show("Press OK upgrade transition rule Dto records in the database, and delete currently invalid records.");

            var allRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll();
            foreach (var cmRule in allRules)
            {
                //cmRule.CMSystemStateId = cmRule.ToCMSystemStateId;

                var opResult = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Update(cmRule);
                if (opResult.Errors.Any())
                {
                    var opDelResult = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Delete(cmRule.Id);
                    if (opDelResult.Errors.Any())
                    {
                        MessageBox.Show(opDelResult.ErrorsCombined);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the BSON data stored in the db to match the current properties available in the Dto
        /// Run this after the Dto has been changed
        /// </summary>
        private static void Upgrade_TaskDto()
        {
            MessageBox.Show("Press OK upgrade task Dto records in the database, and delete currently invalid records.");

            var allTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll();
            foreach (var cmTask in allTasks)
            {
                var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Update(cmTask);
                if (opResult.Errors.Any())
                {
                    var opDelResult = CMDataProvider.DataStore.Value.CMTasks.Value.Delete(cmTask.Id);
                    if (opDelResult.Errors.Any())
                    {
                        MessageBox.Show(opDelResult.ErrorsCombined);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the BSON data stored in the db to match the current properties available in the Dto
        /// Run this after the Dto has been changed
        /// </summary>
        private static void Upgrade_FeatureDto()
        {
            MessageBox.Show("Press OK upgrade feature Dto records in the database, and delete currently invalid records.");

            var allFeatures = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll();
            foreach (var cmFeature in allFeatures)
            {
                var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Update(cmFeature);
                if (opResult.Errors.Any())
                {
                    var opDelResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Delete(cmFeature.Id);
                    if (opDelResult.Errors.Any())
                    {
                        MessageBox.Show(opDelResult.ErrorsCombined);
                    }
                }
            }
        }

        private static void Upgrade_SystemDto()
        {
            MessageBox.Show("Press OK upgrade system Dto records in the database.");

            var allSystems = CMDataProvider.DataStore.Value.CMSystems.Value.GetAll();
            foreach (var cmSystem in allSystems)
            {
                var opResult = CMDataProvider.DataStore.Value.CMSystems.Value.Update(cmSystem);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                }
            }
        }

        private static void Debug_DeleteTasksNotInAFeature()
        {
            MessageBox.Show("Press OK to delete all task that are not in a feature.");

            var allTaskInstances = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Instances();
            var allTaskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Templates();
            var allTasks = allTaskInstances.Union(allTaskTemplates);

            foreach (var cmTask in allTasks)
            {
                if (cmTask.CMFeatureId == 0)
                {
                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Delete(cmTask.Id);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show(opResult.ErrorsCombined);
                    }
                }
            }
        }

        private static void Debug_DeleteInvalidFeatureTransitionRules()
        {
            MessageBox.Show("Press OK to delete all state transition rules that have a feature id of 0 or close the program now.");

            var allStateTransitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll();
            var invalidRules = allStateTransitionRules.Where(r => r.CMFeatureId == 0);
            foreach (var invalidRule in invalidRules)
            {
                var opResult = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.Delete(invalidRule.Id);
                if (opResult.Errors.Any())
                {
                    MessageBox.Show(opResult.ErrorsCombined);
                }
            }
        }

        private static void Debug_DeleteAllTaskAndFeatureInstances()
        {
            MessageBox.Show("Press OK to delete all task and feature instance data or close the program now.");

            var allTaskInstances = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Instances();
            foreach (var taskInstance in allTaskInstances)
            {
                CMDataProvider.DataStore.Value.CMTasks.Value.Delete(taskInstance.Id);
            }

            var allFeatureInstances = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances();
            foreach (var featureInstance in allFeatureInstances)
            {
                CMDataProvider.DataStore.Value.CMFeatures.Value.Delete(featureInstance.Id);
            }
        }
    }
}
