﻿using DataProvider.Events;
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

        public IEnumerable<CMTaskDto> GetAll_Templates()
        {
            var results = Find(t => t.CMParentTaskTemplateId == 0);

            return results;
        }

        public IEnumerable<CMTaskDto> GetAll_Instances()
        {
            var results = Find(t => t.CMParentTaskTemplateId != 0);

            return results;
        }

        public IEnumerable<CMTaskDto> GetAll_ForFeature(int cmFeatureId, bool isTemplate)
        {
            var results = Find(t =>
                (isTemplate ? t.CMParentTaskTemplateId == 0 : t.CMParentTaskTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
             && t.CMFeatureId == cmFeatureId);

            return results.OrderBy(t => t.Title);
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }

            if (dto.CMFeatureId == 0)
            {
                opResult.Errors.Add($"A task must be assigned to a feature in {CollectionName}");
            }

            if (dto.CMSystemStateId == 0)
            {
                opResult.Errors.Add($"A task must be assigned to a system state in {CollectionName}");
            }

            if (dto.CMTaskStateId == 0)
            {
                opResult.Errors.Add($"A task must have a task state set in {CollectionName}");
            }

            if (dto.CMTaskTypeId == 0)
            {
                opResult.Errors.Add($"A task must have a task type set in {CollectionName}");
            }

            var taskStateTemplate = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Template, dto.CMTaskTypeId);

            // The only state option for a template is "Template"
            if (dto.IsTemplate && (dto.CMTaskStateId != taskStateTemplate.Id))
            {
                opResult.Errors.Add($"A task template task state must be set to the 'Template' state.");
            }

            // A task instance cannot be set to the "Template" state.
            if (!dto.IsTemplate && (dto.CMTaskStateId == taskStateTemplate.Id))
            {
                opResult.Errors.Add($"A task instance cannot be set to the 'Template' state.");
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
    }
}
