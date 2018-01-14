using DataProvider.Events;
using Dto;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMTasksCRUD : CMDataProviderCRUDBase<CMTaskDto>
    {
        public CMTasksCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Gets the ad-hoc task template for the specified task type. If one doesn't exist in the database it will be created.
        /// </summary>
        /// <param name="cmTaskTypeId"></param>
        /// <returns></returns>
        public CMTaskDto Get_AdHocTemplate(int cmTaskTypeId)
        {
            var systemFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.GetInternalFeature();

            var adHocTemplate = Find(t =>
                t.CMFeatureId == systemFeature.Id
                && t.CMTaskTypeId == cmTaskTypeId
                ).FirstOrDefault();

            if (adHocTemplate == null)
            {
                var newAdhocTemplate = new CMTaskDto()
                {
                    CMFeatureId = systemFeature.Id,
                    CMTaskTypeId = cmTaskTypeId,

                    // These values should not be cloned when creating an instance, but we need valid values
                    CMSystemStateId = systemFeature.CMSystemStateId,
                    CMTaskStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Template, cmTaskTypeId).Id,
                    Title = Guid.NewGuid().ToString()
                };
                var opResult = Insert(newAdhocTemplate);
                if (opResult.Errors.Any())
                {
                    // Not expecting any errors here, but set an alarm just in case
                    throw new Exception(opResult.ErrorsCombined);
                }
                adHocTemplate = Find(t =>
                    t.CMFeatureId == systemFeature.Id
                    && t.CMTaskTypeId == cmTaskTypeId
                    ).First();
            }

            return adHocTemplate;
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

        public IEnumerable<CMTaskDto> GetAll_ForFeature(int cmFeatureId)
        {
            var results = Find(t => t.CMFeatureId == cmFeatureId);

            var cmFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(cmFeatureId);
            int cmFeatureTemplateId = cmFeature.IsTemplate ? cmFeature.Id : cmFeature.CMParentFeatureTemplateId;
            var systemStatesLookup = CMDataProvider.DataStore.Value.CMSystemStates.Value.GetAll_ForFeatureTemplate(cmFeatureTemplateId)
                .ToDictionary(s => s.Id, s => s);

            return results.OrderBy(t => systemStatesLookup[t.CMSystemStateId].Priority)
                    .ThenBy(t => t.ExecutionOrder);
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

            // Must pass the previous checks before going on to this next phase of checks
            if (opResult.Errors.Any())
            {
                return opResult;
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
