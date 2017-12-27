using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMTaskStatesCRUD : CMDataProviderCRUDBase<CMTaskStateDto>
    {
        public const string InternalState_Complete = "Complete"; // mcbtodo: I think a better internal state to represent this would be "Closed"
        public const string InternalState_Template = "Template";
        public const string InternalState_Instance = "Instance";

        public List<string> InternalStates { get; set; } = new List<string>()
        {
            InternalState_Template,
            InternalState_Instance,
            InternalState_Complete
        };

        public CMTaskStatesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        public new IEnumerable<CMTaskStateDto> GetAll()
        {
            var results = base.GetAll();
            return results;
        }

        public IEnumerable<CMTaskStateDto> GetAll_ForTaskType(int cmTaskTypeId)
        {
            var results = base.Find(s => s.TaskTypeId == cmTaskTypeId);
            return results.OrderBy(t => t.Priority);
        }

        public CMTaskStateDto Get_ForInternalName(string name, int cmTaskTypeId)
        {
            var results = Find(s => 
                s.TaskTypeId == cmTaskTypeId
                && s.InternalName.Equals(name, System.StringComparison.Ordinal)
                );
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMTaskStateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.InternalName) || string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }
            if (dto.TaskTypeId == 0)
            {
                opResult.Errors.Add($"An item in {CollectionName} must have the task type specified.");
            }

            return opResult;
        }

        public override CMCUDResult Insert(CMTaskStateDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
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
