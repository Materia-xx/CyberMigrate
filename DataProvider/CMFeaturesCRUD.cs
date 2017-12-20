using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeaturesCRUD : CMDataProviderCRUDBase<CMFeatureDto>
    {
        public CMFeaturesCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
        {
        }

        /// <summary>
        /// Finds the first feature by name under the given system.
        /// There should be only 1 if the program is working correct in keeping duplicate names out
        /// </summary>
        /// <param name="featureName"></param>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public CMFeatureDto Get_ForName(string featureName, int cmSystemId, bool isTemplate)
        {
            // mcbtodo: Query.EQ just isn't doing what we want here, recode it again to pass in lambdas

            // First get all within the specified system
            var results = GetAll_ForSystem(cmSystemId, isTemplate);

            // The filter it down to just the one in the specified name, if it exists
            results = results.Where(f => f.Name.Equals(featureName, System.StringComparison.OrdinalIgnoreCase));
            return results.FirstOrDefault();
        }

        public IEnumerable<CMFeatureDto> GetAll_ForSystem(int cmSystemId, bool isTemplate)
        {
            Query query = Query.EQ(nameof(CMFeatureDto.CMSystemId), cmSystemId);
            var results = QueryCollection(query);

            results = results.Where(f => f.IsTemplate == isTemplate);

            return results.OrderBy(f => f.Name);
        }
    }
}
