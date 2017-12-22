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

        public override CMCUDResult Insert(CMTaskFactoryDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (string.IsNullOrWhiteSpace(insertingObject.Name))
            {
                opResult.Errors.Add($"Cannot insert a new item into {CollectionName} because the name is empty.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }
    }
}
