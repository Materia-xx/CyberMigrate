using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBase.Extensions
{
    public static class StateCalculations
    {
        private static Dictionary<int, List<CMFeatureStateTransitionRuleDto>> transitionRulesByFeatureId;
        private static Dictionary<int, List<CMTaskDto>> tasksByFeatureId;
        private static Dictionary<int, int> closedTaskStateByTaskTypeId;

        /// <summary>
        /// Indicates that the lookup references need to be refreshed.
        /// </summary>
        public static bool LookupsRefreshNeeded { get; set; } = true;

        /// <summary>
        /// Recalculates the system state for all feature instances
        /// </summary>
        /// <returns>Returns true if any feature state changed</returns>
        public static bool CalculateAllFeatureStates()
        {
            var allFeatureInstances = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances();
            var anyFeatureUpdated = false;
            foreach (var cmFeature in allFeatureInstances)
            {
                if (cmFeature.RecalculateSystemState())
                {
                    anyFeatureUpdated = true;
                }
            }

            return anyFeatureUpdated;
        }

        /// <summary>
        /// Processes the state transition rules for the feature and sets the new system state if appropriate.
        /// </summary>
        /// <param name="cmFeature"></param>
        /// <returns>
        /// Returns true if the state was changed.
        /// If the feature is already in the db it will be updated there as well.
        /// If the feature has not yet been inserted, only the object passed in will contain the updated state.
        /// </returns>
        public static bool RecalculateSystemState(this CMFeatureDto cmFeature)
        {
            // State is not calculated for feature templates
            if (cmFeature.CMParentFeatureTemplateId == 0)
            {
                return false;
            }

            // Refresh the lookups if needed
            RefreshLookups();

            var transitionRules = transitionRulesByFeatureId.ContainsKey(cmFeature.CMParentFeatureTemplateId) ?
                transitionRulesByFeatureId[cmFeature.CMParentFeatureTemplateId] :
                new List<CMFeatureStateTransitionRuleDto>();

            if (!transitionRules.Any())
            {
                throw new Exception("Unable to set a default system state on the feature because no state transition rules are set up yet for this feature type.");
            }

            // If the logic falls through the loop below and doesn't match anything, default to the last state listed
            int shouldBeSystemStateId = transitionRules.Last().CMSystemStateId;

            foreach (var transitionRule in transitionRules)
            {
                var tasksInReferencedSystemState = tasksByFeatureId.ContainsKey(cmFeature.Id) ?
                    tasksByFeatureId[cmFeature.Id].Where(t => t.CMSystemStateId == transitionRule.CMSystemStateId) :
                    new List<CMTaskDto>();

                // A feature with no tasks is the same as a feature that has all of it's tasks closed. As far as this logic goes.
                // Same idea for: A system state (within a feature) that has no tasks considers that system state to have all of it's tasks closed.
                // Therefore when creating a new feature, before the tasks are added, the feature will default to the lowest priority state.
                if (tasksInReferencedSystemState.Any(cmTask =>
                {
                    var closedTaskStateId = closedTaskStateByTaskTypeId[cmTask.CMTaskTypeId];
                    return cmTask.CMTaskStateId != closedTaskStateId;
                }))
                {
                    shouldBeSystemStateId = transitionRule.CMSystemStateId;
                    break; // Stop looping through the rules as soon as we find a state that has unclosed tasks
                }
            }

            // mcbtodo: see if there is an easy way that works to wrap this checking and update logic into extension methods. It would handle updating *just* the system state id and keeping whatever title is currently in the db.
            if (cmFeature.CMSystemStateId != shouldBeSystemStateId)
            {
                cmFeature.CMSystemStateId = shouldBeSystemStateId;
                if (cmFeature.Id != 0)
                {
                    // Refresh the feature data before we check as a callback on another thread may have updated it
                    cmFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmFeature.Id);
                    cmFeature.CMSystemStateId = shouldBeSystemStateId;

                    var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Update(cmFeature);
                    if (opResult.Errors.Any())
                    {
                        throw new Exception(opResult.ErrorsCombined);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static void RefreshLookups()
        {
            if (!LookupsRefreshNeeded)
            {
                return;
            }

            transitionRulesByFeatureId = new Dictionary<int, List<CMFeatureStateTransitionRuleDto>>();
            tasksByFeatureId = new Dictionary<int, List<CMTaskDto>>();
            closedTaskStateByTaskTypeId = new Dictionary<int, int>();

            var allStateTransitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll();
            var allFeatureTemplates = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Templates();
            var allFeatureInstances = CMDataProvider.DataStore.Value.CMFeatures.Value.GetAll_Instances();

            var allTaskInstances = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_Instances();
            var allTaskTypes = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll();

            foreach (var cmFeatureTemplate in allFeatureTemplates)
            {
                var featureRules = allStateTransitionRules.Where(s => s.CMFeatureId == cmFeatureTemplate.Id).OrderBy(s => s.Order);
                transitionRulesByFeatureId[cmFeatureTemplate.Id] = featureRules.ToList();

                var tasks = allTaskInstances.Where(s => s.CMFeatureId == cmFeatureTemplate.Id);
                tasksByFeatureId[cmFeatureTemplate.Id] = tasks.ToList();
            }

            foreach (var cmFeatureInstance in allFeatureInstances)
            {
                var tasks = allTaskInstances.Where(s => s.CMFeatureId == cmFeatureInstance.Id);
                tasksByFeatureId[cmFeatureInstance.Id] = tasks.ToList();
            }

            foreach (var taskType in allTaskTypes)
            {
                var closedState = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Closed, taskType.Id);
                closedTaskStateByTaskTypeId[taskType.Id] = closedState.Id;
            }

            LookupsRefreshNeeded = false;
        }
    }
}
