using Dto;
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
                return default(IEnumerable<CMFeatureStateTransitionRuleDto>);
            }

            var results = Find(r =>
                r.CMFeatureId == cmFeatureId
            );

            // Always return rules so that the first one in the list that the program comes across is the winner and the rest are ignored
            // This also has the side effect of listing them in this same order in the configuration UI.
            return results.OrderBy(r => r.Priority);
        }

        public override CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();
            // Check to see if there are any task templates that reference the state referred to in this rule
            var cmRule = Get(deletingId);
            var taskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(cmRule.CMFeatureId, true);
            if (taskTemplates.Any(t => t.CMSystemStateId == cmRule.ToCMSystemStateId))
            {
                opResult.Errors.Add($"Cannot delete {CollectionName} with id {deletingId} because there are currently task templates in the state it referrs to.");
                return opResult;
            }

            return base.Delete(deletingId);
        }
    }
}
