using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBase.Extensions
{
    public static class TaskExtensions
    {
        public static CMFeatureDto CreateFeatureInstance(this CMFeatureDto featureTemplate)
        {
            if (!featureTemplate.IsTemplate)
            {
                throw new InvalidOperationException("Cannot create an instance of an already instanced feature.");
            }

            // Clone the feature template to a feature instance
            var featureDto = new CMFeatureDto()
            {
                CMParentFeatureTemplateId = featureTemplate.Id,
                CMSystemId = featureTemplate.CMSystemId,
                IsTemplate = false,
                Name = featureTemplate.Name // mcbtodo: apply template vars here when they are implemented
            };

            var opResultFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Insert(featureDto);
            if (opResultFeature.Errors.Any())
            {
                throw new Exception(opResultFeature.ErrorsCombined);
            }

            // Clone each task in the feature template
            var taskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(featureTemplate.Id, true);
            foreach (var taskTemplate in taskTemplates)
            {
                var taskTemplateType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(taskTemplate.CMTaskTypeId);

                // We can clone the task template to instance here
                var cmTaskInstance = new CMTaskDto()
                {
                    CMParentTaskTemplateId = taskTemplate.Id,
                    CMFeatureId = featureDto.Id,
                    CMSystemStateId = taskTemplate.CMSystemStateId,
                    CMTaskStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(CMTaskStatesCRUD.InternalState_Instance, taskTemplateType.Id).Id,
                    CMTaskTypeId = taskTemplate.CMTaskTypeId,
                    IsTemplate = false,
                    Title = taskTemplate.Title // mcbtodo: apply template vars here when they are implemented
                };
                var opResultTask = CMDataProvider.DataStore.Value.CMTasks.Value.Insert(cmTaskInstance);
                if (opResultTask.Errors.Any())
                {
                    throw new Exception(opResultTask.ErrorsCombined);
                }

                // For the task data we revert to the task factory to provide it
                TaskFactoriesCatalog.Instance.CreateTaskDataInstance(taskTemplateType, cmTaskInstance);
            }

            return featureDto;
        }
    }
}
