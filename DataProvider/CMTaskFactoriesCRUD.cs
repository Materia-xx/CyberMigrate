using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMTaskFactoriesCRUD : CMDataProviderCRUDBase<CMTaskFactoryDto>
    {
        public CMTaskFactoriesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public new IEnumerable<CMTaskFactoryDto> GetAll()
        {
            return base.GetAll();
        }

        public CMTaskFactoryDto Get_ForName(string taskFactoryName)
        {
            var results = Find(f => f.Name.Equals(taskFactoryName, System.StringComparison.OrdinalIgnoreCase));
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMTaskFactoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }

            return opResult;
        }

        public override CMCUDResult Insert(CMTaskFactoryDto insertingObject)
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
