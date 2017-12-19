using CyberMigrateCommom;
using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeatureStateTransitionRulesCRUD : CMDataProviderCRUD<CMFeatureStateTransitionRule>
    {
        public CMFeatureStateTransitionRulesCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
        {
        }

        public IEnumerable<CMFeatureStateTransitionRule> GetAll_ForFeatureTemplate(int cmFeatureTemplateId)
        {
            Query query = Query.EQ(nameof(CMFeatureStateTransitionRule.CMFeatureTemplateId), cmFeatureTemplateId);
            var results = QueryCollection(query);

            // Always return rules so that the first one in the list that the program comes across is the winner and the rest are ignored
            // This also has the side effect of listing them in this same order in the configuration UI.
            return results.OrderBy(r => r.Priority);
        }
    }
}
