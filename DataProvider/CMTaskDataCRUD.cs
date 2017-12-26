using Dto;
using LiteDB;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// Provides a templated CRUD interface for task data of the specified type
    /// </summary>
    public class CMTaskDataCRUD<T> : CMDataProviderCRUDBase<T> where T : CMTaskDataDtoBase
    {
        public CMTaskDataCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public T Get_ForTaskId(int taskId)
        {
            var results = Find(d => d.TaskId == taskId);
            return results.FirstOrDefault();
        }

        public CMCUDResult Delete_ForTaskId(int taskId)
        {
            var opResult = new CMCUDResult();
            var result = Get_ForTaskId(taskId);
            if (result == null)
            {
                return opResult;
            }
            return base.Delete(result.Id);
        }

        private CMCUDResult UpsertChecks(CMCUDResult opResult, T dto)
        {
            if (dto.TaskId == 0)
            {
                opResult.Errors.Add($"Item in collection {CollectionName} must have the {nameof(dto.TaskId)} set before insert or update.");
            }

            return opResult;
        }

        public override CMCUDResult Insert(T insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            if (Get_ForTaskId(insertingObject.TaskId) != null)
            {
                opResult.Errors.Add($"A pre-existing task data record exists for task id {insertingObject.TaskId} in collection {CollectionName}. Update that record instead of adding a new one.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(T updatingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, updatingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            var dbTaskData = Get(updatingObject.Id);
            if (dbTaskData.TaskId != updatingObject.TaskId)
            {
                // Allow changing the task that the data is attached to, but it must be done in a way where there is never 1 task with 2 task data records
                if (Get_ForTaskId(updatingObject.TaskId) != null)
                {
                    opResult.Errors.Add($"A pre-existing task data record exists for task id {updatingObject.TaskId} in collection {CollectionName} therefore the updating task data cannot be re-assigned to that task. First make sure the destination task has no task data.");
                    return opResult;
                }
            }

            return base.Update(updatingObject);
        }
    }
}
