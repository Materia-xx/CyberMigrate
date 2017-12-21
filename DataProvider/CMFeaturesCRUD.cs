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
            var results = Find(f => 
                f.IsTemplate == isTemplate
             && f.CMSystemId == cmSystemId
             && f.Name.Equals(featureName, System.StringComparison.OrdinalIgnoreCase));

            return results.FirstOrDefault();
        }

        public IEnumerable<CMFeatureDto> GetAll_ForSystem(int cmSystemId, bool isTemplate)
        {
            var results = Find(f =>
                f.IsTemplate == isTemplate
             && f.CMSystemId == cmSystemId);

            return results.OrderBy(f => f.Name);
        }
    }
}
