using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMTasksCRUD : CMDataProviderCRUDBase<CMTaskDto>
    {
        public CMTasksCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public IEnumerable<CMTaskDto> GetAll_ForFeature(int cmFeatureId, bool isTemplate)
        {
            var results = Find(t =>
                t.IsTemplate == isTemplate
             && t.CMFeatureId == cmFeatureId);

            return results.OrderBy(t => t.Name);
        }

        public override CMCUDResult Insert(CMTaskDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (string.IsNullOrWhiteSpace(insertingObject.Name))
            {
                opResult.Errors.Add($"Name cannot be empty when inserting a new item into {CollectionName}");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        //    // mcbtodo: Add an override to delete in the Tasks provider that takes care of also deleting any associated task data when a delete happens.
    }
}
