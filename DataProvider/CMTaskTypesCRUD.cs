using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMTaskTypesCRUD : CMDataProviderCRUDBase<CMTaskTypeDto>
    {
        public CMTaskTypesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public new IEnumerable<CMTaskTypeDto> GetAll()
        {
            return base.GetAll();
        }

        public CMTaskTypeDto Get_ForName(string taskTypeName)
        {
            var results = Find(f => f.Name.Equals(taskTypeName, System.StringComparison.OrdinalIgnoreCase));
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMTaskTypeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }

            return opResult;
        }

        public override CMCUDResult Insert(CMTaskTypeDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Insert(insertingObject);
        }

    }
}
