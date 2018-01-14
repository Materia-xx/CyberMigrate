using DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CyberMigrate
{
    public static class DBMaintenance
    {
        private static Dictionary<int, Func<bool>> UpgradeFunctions = new Dictionary<int, Func<bool>>();

        private static void AssignUpgradeFunctions()
        {
            // v2 - Adds MigrationOrder to system states. To differiantiate from the state Priority.
            UpgradeFunctions[2] = () =>
            {
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

                return true;
            };

            // v3 - Adds task factory version so task factories can implement database upgrades also
            UpgradeFunctions[3] = () =>
            {
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

                return true;
            };

            // v4 - Adds color of task backgrounds to the feature dto
            UpgradeFunctions[4] = () =>
            {
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

                return true;
            };

            // v5 - Adds task ExecutionOrder to hint at the order that tasks should be completed in within their system state
            UpgradeFunctions[5] = () =>
            {
                // Set all current tasks ExecutionOrder to 0 to update the bson data. They'll need to manually be adjusted in the UI afterwards.
                var allTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll();
                foreach (var cmTask in allTasks)
                {
                    cmTask.ExecutionOrder = 0;
                    var opResult = CMDataProvider.DataStore.Value.CMTasks.Value.Update(cmTask);
                    if (opResult.Errors.Any())
                    {
                        MessageBox.Show($"An unrecoverable database upgrade error has occurred:\r\n{opResult.ErrorsCombined}");
                        return false;
                    }
                }

                return true;
            };
        }

        public static bool RunMaintenanceRoutines()
        {
            AssignUpgradeFunctions();

            // Gets the schema version that the database is currently at. If unknown defaults to the first supported version.
            var databaseSchemaVersion = CMDataProvider.DataStore.Value.GetDatabaseSchemaVersion();

            // Get a list of all upgrade functions available
            var allUpgradeVersions = UpgradeFunctions.Keys.OrderBy(v => v);

            foreach (var upgradeVersion in allUpgradeVersions)
            {
                // If the db is not yet at this version, the prompt and do the upgrade
                if (databaseSchemaVersion < upgradeVersion)
                {
                    var userResponse = MessageBox.Show($"Upgrading database schema to version {upgradeVersion}. Press OK to continue or cancel to quit.", $"Upgrade to schema version {upgradeVersion}", MessageBoxButton.OKCancel);
                    if (userResponse != MessageBoxResult.OK)
                    {
                        return false;
                    }

                    // Do the upgrade
                    bool upgradeSuccess = UpgradeFunctions[upgradeVersion]();
                    if (!upgradeSuccess)
                    {
                        return false;
                    }

                    // Set the db to now be the upgraded version
                    CMDataProvider.DataStore.Value.SetDatabaseSchemaVersion(upgradeVersion);
                    databaseSchemaVersion = CMDataProvider.DataStore.Value.GetDatabaseSchemaVersion();
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

        // -------------------------------------------

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
