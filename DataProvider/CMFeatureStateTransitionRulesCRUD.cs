﻿using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeatureStateTransitionRulesCRUD : CMDataProviderCRUDBase<CMFeatureStateTransitionRuleDto>
    {
        public CMFeatureStateTransitionRulesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public IEnumerable<CMFeatureStateTransitionRuleDto> GetAll_ForFeatureTemplate(int cmFeatureId)
        {
            var featureTemplate = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmFeatureId);

            // If the feature doesn't exist or it isn't a template then don't even look at the available state transition rules
            if (featureTemplate == null || featureTemplate.IsTemplate == false)
            {
                return Enumerable.Empty<CMFeatureStateTransitionRuleDto>();
            }

            var results = Find(r =>
                r.CMFeatureId == cmFeatureId
            );

            // Always return rules so that the first one in the list that the program comes across is the winner and the rest are ignored
            // This also has the side effect of listing them in this same order in the configuration UI.
            return results.OrderBy(r => r.Order);
        }

        public IEnumerable<CMFeatureStateTransitionRuleDto> GetAll_ThatRef_ToCMSystemStateId(int cmSystemStateId)
        {
            return  Find(r => r.CMSystemStateId == cmSystemStateId);
        }

        public IEnumerable<CMFeatureStateTransitionRuleDto> GetAll_ThatRef_SystemStateId(int cmSystemStateId)
        {
            return Find(r => r.CMSystemStateId == cmSystemStateId);
        }

        public override CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();

            var cmRule = Get(deletingId);

            // How many other transition rules in this feature refer to the state ?
            var matchingRules = Find(r => 
                r.CMSystemStateId == cmRule.CMSystemStateId
                && r.CMFeatureId == cmRule.CMFeatureId
                && r.Id != deletingId
                );

            // If the one being deleted is the last one then check to see if any task templates are under this state
            if (!matchingRules.Any())
            {
                var taskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(cmRule.CMFeatureId);
                if (taskTemplates.Any(t => t.CMSystemStateId == cmRule.CMSystemStateId))
                {
                    var targetSystemState = CMDataProvider.DataStore.Value.CMSystemStates.Value.Get(cmRule.CMSystemStateId);

                    opResult.Errors.Add($"Cannot delete item from {CollectionName} with id {deletingId} that has a target state of {targetSystemState.Name} because there are currently task templates in the state it referrs to.");
                    return opResult;
                }
            }

            return base.Delete(deletingId);
        }

        public override CMCUDResult Update(CMFeatureStateTransitionRuleDto updatingObject)
        {
            var opResult = new CMCUDResult();

            var cmRule = Get(updatingObject.Id);

            // Is the state that it's referring to changing ?
            if (cmRule.CMSystemStateId != updatingObject.CMSystemStateId)
            {
                // How many other transition rule states in this feature refer to the state ?
                var matchingRules = Find(r =>
                    r.CMSystemStateId == cmRule.CMSystemStateId
                    && r.CMFeatureId == cmRule.CMFeatureId
                    && r.Id != updatingObject.Id
                    );

                // If the one being updated was the last rule that referred to the state then check to see if any task templates are under this state
                if (!matchingRules.Any())
                {
                    var taskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(cmRule.CMFeatureId);
                    if (taskTemplates.Any(t => t.CMSystemStateId == cmRule.CMSystemStateId))
                    {
                        opResult.Errors.Add($"Cannot update item in {CollectionName} with id {updatingObject.Id} because there are currently task templates in the state it referrs to.");
                        return opResult;
                    }
                }
            }

            return base.Update(updatingObject);
        }
    }
}
