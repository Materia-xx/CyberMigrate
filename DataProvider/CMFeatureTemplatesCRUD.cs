using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeatureTemplatesCRUD : CMDataProviderCRUD<CMFeatureTemplate>
    {
        public CMFeatureTemplatesCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
        {
        }

        /// <summary>
        /// Finds the first feature template by name under the given system.
        /// There should be only 1 if the program is working correct in keeping duplicate names out
        /// </summary>
        /// <param name="featureTemplateName"></param>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public CMFeatureTemplate Get_ForFeatureTemplateName(string featureTemplateName, int cmSystemId)
        {
            // First get all within the specified system
            var results = GetAll_ForSystem(cmSystemId);

            // The filter it down to just the one in the specified name, if it exists
            results = results.Where(f => f.Name.Equals(featureTemplateName, System.StringComparison.OrdinalIgnoreCase));
            return results.FirstOrDefault();
        }

        public IEnumerable<CMFeatureTemplate> GetAll_ForSystem(int cmSystemId)
        {
            Query query = Query.EQ(nameof(CMFeatureTemplate.CMSystemId), cmSystemId);
            var results = QueryCollection(query);

            return results.OrderBy(f => f.Name);
        }
    }
}
