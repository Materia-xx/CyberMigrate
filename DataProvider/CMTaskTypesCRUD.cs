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

        public CMTaskTypeDto Get_ForName(string taskTypeName)
        {
            var results = Find(f => f.Name.Equals(taskTypeName, System.StringComparison.OrdinalIgnoreCase));
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Attempts to get the task type record from a task id
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public CMTaskTypeDto Get_ForTaskId(int taskId)
        {
            // Try to get a ref to the task
            var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(taskId);
            if (cmTask == null)
            {
                return null;
            }

            // Try to figure out what the task type is
            var cmTaskType = Get(cmTask.CMTaskTypeId);
            return cmTaskType;
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
