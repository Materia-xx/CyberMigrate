using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeatureStateTransitionRulesCRUD : CMDataProviderCRUDBase<CMFeatureStateTransitionRuleDto>
    {
        public CMFeatureStateTransitionRulesCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
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

            Query query = Query.EQ(nameof(CMFeatureStateTransitionRuleDto.CMFeatureId), cmFeatureId);
            var results = QueryCollection(query);

            // Always return rules so that the first one in the list that the program comes across is the winner and the rest are ignored
            // This also has the side effect of listing them in this same order in the configuration UI.
            return results.OrderBy(r => r.Priority);
        }

        public void DeleteAll_ForFeatureTemplate(int cmFeatureTemplateId)
        {
            var results = GetAll_ForFeatureTemplate(cmFeatureTemplateId);

            foreach (var rule in results)
            {
                base.Delete(rule.Id);
            }
        }
    }
}
