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

            return results.OrderBy(t => t.Title);
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMTaskDto dto) // mcbtodo: do this in all CRUD
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                opResult.Errors.Add($"Name cannot be empty when inserting a new item into {CollectionName}");
            }

            if (dto.CMTaskTypeId == 0)
            {
                opResult.Errors.Add($"A task must have a task type set in {CollectionName}");
            }

            if (dto.CMTaskStateId == 0)
            {
                opResult.Errors.Add($"A task must have a task state set in {CollectionName}");
            }

            return opResult;
        }

        public override CMCUDResult Insert(CMTaskDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMTaskDto updatingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, updatingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Update(updatingObject);
        }

        //    // mcbtodo: Add an override to delete in the Tasks provider that takes care of also deleting any associated task data when a delete happens.
    }
}
