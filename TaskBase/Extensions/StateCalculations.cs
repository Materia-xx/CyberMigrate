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

            // A feature that is not entered in the db yet is a special case.
            // It won't have any tasks in any of the states and will resolve to false on all condition checks, falling completely through them all if we let it go through the loop below.
            // Here we default it to the first highest priority state available if possible
            if (cmFeature.Id == 0)
            {
                if (!transitionRules.Any())
                {
                    throw new Exception("Unable to set a default system state on the feature because no state transition rules are set up yet for this feature type.");
                }

                // The rules are already in order by the priority
                var highestPriorityRule = transitionRules.First();

                if (cmFeature.CMSystemStateId == highestPriorityRule.ToCMSystemStateId)
                {
                    return false;
                }

                cmFeature.CMSystemStateId = highestPriorityRule.ToCMSystemStateId;
                return true;
            }

            foreach (var transitionRule in transitionRules)
            {
                var tasksInQuerySystemState = tasksByFeatureId.ContainsKey(cmFeature.Id) ?
                    tasksByFeatureId[cmFeature.Id].Where(t => t.CMSystemStateId == transitionRule.ConditionQuerySystemStateId) :
                    new List<CMTaskDto>();

                // Defaults for when there are no tasks
                // - Any tasks that are closed: false
                // - Any tasks that are not closed: false
                // - All tasks are closed: false
                // - All tasks are not closed: false
                // Therefore a system state with no tasks will never meet the condition
                bool meetsRuleCondition = false;
                if (tasksInQuerySystemState.Any())
                {
                    int meetsConditionCount = 0;

                    foreach (var cmTask in tasksInQuerySystemState)
                    {
                        var closedTaskStateId = closedTaskStateByTaskTypeId[cmTask.CMTaskTypeId];

                        if ((cmTask.CMTaskStateId == closedTaskStateId) == transitionRule.ConditionTaskClosed)
                        {
                            meetsConditionCount++;

                            // If we're only checking for "Any" tasks that meet the condition we can break early as we found 1.
                            if (!transitionRule.ConditionAllTasks)
                            {
                                break;
                            }
                        }
                    }

                    if (transitionRule.ConditionAllTasks && meetsConditionCount == tasksInQuerySystemState.Count())
                    {
                        meetsRuleCondition = true;
                    }
                    else if (!transitionRule.ConditionAllTasks && meetsConditionCount > 0)
                    {
                        meetsRuleCondition = true;
                    }
                }

                // If this transition rule met the condition then move the feature to the target state if it is not already there
                // Rule processing stops as soon as we find one that meets the conditions
                if (meetsRuleCondition)
                {
                    if (cmFeature.CMSystemStateId != transitionRule.ToCMSystemStateId)
                    {
                        cmFeature.CMSystemStateId = transitionRule.ToCMSystemStateId;
                        var opResult = CMDataProvider.DataStore.Value.CMFeatures.Value.Update(cmFeature);
                        if (opResult.Errors.Any())
                        {
                            throw new Exception(opResult.ErrorsCombined);
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
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
                var featureRules = allStateTransitionRules.Where(s => s.CMFeatureId == cmFeatureTemplate.Id).OrderBy(s => s.Priority);
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
                var closedState = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(CMTaskStatesCRUD.InternalState_Closed, taskType.Id);
                closedTaskStateByTaskTypeId[taskType.Id] = closedState.Id;
            }

            LookupsRefreshNeeded = false;
        }
    }
}
