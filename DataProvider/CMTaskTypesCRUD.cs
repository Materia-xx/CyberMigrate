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

        public override CMCUDResult Insert(CMTaskTypeDto insertingObject)
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
