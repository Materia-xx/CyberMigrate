using DataProvider;
using System.Linq;
using System.Windows;

namespace CyberMigrate
{
    public static class DBMaintenance
    {
        public static void RunMaintenanceRoutines()
        {
            //Upgrade_TaskDto();
            //Upgrade_FeatureDto();
            //Upgrade_TransitionRuleDto();

            // mcbtodo: These are only cleanup routines I'm using to clean up the db during dev and won't be needed in the released version
            //Debug_DeleteTasksNotInAFeature();
            //Debug_DeleteInvalidFeatureTransitionRules();
            //Debug_DeleteAllTaskAndFeatureInstances();
        }

        private static void Upgrade_TransitionRuleDto()
        {
            MessageBox.Show("Press OK upgrade transition rule Dto records in the database, and delete currently invalid records.");

            var allRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll();
            foreach (var cmRule in allRules)
            {
                //cmRule.ConditionTaskClosed = cmRule.ConditionTaskComplete;

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
