using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMTaskStatesCRUD : CMDataProviderCRUDBase<CMTaskStateDto>
    {
        public CMTaskStatesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public IEnumerable<CMTaskStateDto> GetAll_ForTaskType(int cmTaskTypeId)
        {
            var results = base.Find(s => s.TaskTypeId == cmTaskTypeId);
            return results;
        }

        public CMTaskStateDto Get_ForPluginTaskStateName(string pluginTaskStateName, int cmTaskTypeId)
        {
            var results = Find(s => 
                s.TaskTypeId == cmTaskTypeId
                && s.InternalName.Equals(pluginTaskStateName, System.StringComparison.OrdinalIgnoreCase)
                );
            return results.FirstOrDefault();
        }

        public override CMCUDResult Insert(CMTaskStateDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (string.IsNullOrWhiteSpace(insertingObject.InternalName) || string.IsNullOrWhiteSpace(insertingObject.DisplayName))
            {
                opResult.Errors.Add($"Cannot insert a new item into {CollectionName} because the name is empty.");
                return opResult;
            }

            if (insertingObject.TaskTypeId == 0)
            {
                opResult.Errors.Add($"Cannot insert a new item into {CollectionName} because the task type is not specified.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();

            var dbTaskState = Get(deletingId);
            if (dbTaskState.Reserved)
            {
                opResult.Errors.Add($"Unable to delete task state {dbTaskState.DisplayName} because it is marked as reserved.");
                return opResult;
            }

            return base.Delete(deletingId); 
        }
    }
}
