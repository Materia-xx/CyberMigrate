using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeaturesCRUD : CMDataProviderCRUDBase<CMFeatureDto>
    {
        public CMFeaturesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
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
             && f.Name.Equals(featureName, System.StringComparison.Ordinal)); // Sensitive case allows the user to more easily rename items by just the case

            return results.FirstOrDefault();
        }

        public IEnumerable<CMFeatureDto> GetAll_ForSystem(int cmSystemId, bool isTemplate)
        {
            var results = Find(f =>
                f.IsTemplate == isTemplate
             && f.CMSystemId == cmSystemId);

            return results.OrderBy(f => f.Name);
        }

        public int GetCount_InSystem(int cmSystemId)
        {
            var results = Count(f =>
                f.CMSystemId == cmSystemId);

            return results;
        }

        public override CMCUDResult Insert(CMFeatureDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (Get_ForName(insertingObject.Name, insertingObject.CMSystemId, insertingObject.IsTemplate) != null)
            {
                opResult.Errors.Add($"A feature with the name '{insertingObject.Name}' already exists within the system. Rename that one first.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMFeatureDto updatingObject)
        {
            var opResult = new CMCUDResult();

            if (Get_ForName(updatingObject.Name, updatingObject.CMSystemId, updatingObject.IsTemplate) != null)
            {
                opResult.Errors.Add($"A feature with the name '{updatingObject.Name}' already exists within the system.");
                return opResult;
            }

            return base.Update(updatingObject);
        }
    }
}
