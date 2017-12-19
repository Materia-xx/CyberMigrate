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
            return GetAll().Where(r => r.CMFeatureTemplateId == cmFeatureTemplateId);
        }
    }
}
