using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBase.Extensions
{
    public static class InstancingExtensions
    {
        /// <summary>
        /// Creates an instance of a feature template.
        /// </summary>
        /// <param name="featureTemplate"></param>
        /// <param name="featureDepth">
        /// For situations where we clone entire branches of feature templates, this indicates how many features from the starting 
        /// point we are currently at. If the depth goes over a threshhold the program will halt the cloning operation to 
        /// prevent a never-ending loop.
        /// </param>
        /// <returns></returns>
        public static CMFeatureDto CreateFeatureInstance(this CMFeatureDto featureTemplate, int featureDepth)
        {
            if (!featureTemplate.IsTemplate)
            {
                throw new InvalidOperationException("Cannot create an instance of an already instanced feature.");
            }

            featureDepth++;
            if (featureDepth > 10) // mcbtodo: make this configurable in the master options
            {
                // mcbtodo: this will leave a mess of features and records that needs to be cleaned up, make that easier to deal with.
                throw new StackOverflowException("Unable to clone a feature template that is more than 10 levels deep. Check the feature for errors and try again.");
            }

            // Clone the feature template to a feature instance
            var featureDto = new CMFeatureDto()
            {
                CMParentFeatureTemplateId = featureTemplate.Id,
                CMSystemId = featureTemplate.CMSystemId,
                Name = featureTemplate.Name // mcbtodo: apply template vars here when they are implemented
            };

            // Calculate a default feature state, which is needed to insert into the db
            // Due to the fact that the feature has no cloned tasks yet, this state is likely temporary and will be updated again below
            featureDto.RecalculateSystemState();

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
                    Title = taskTemplate.Title // mcbtodo: apply template vars here when they are implemented
                };
                var opResultTask = CMDataProvider.DataStore.Value.CMTasks.Value.Insert(cmTaskInstance);
                StateCalculations.LookupsRefreshNeeded = true; // mcbtodo: move this into a callback that happens upon task insert
                if (opResultTask.Errors.Any())
                {
                    throw new Exception(opResultTask.ErrorsCombined);
                }

                // For the task data we revert to the task factory to provide it
                TaskFactoriesCatalog.Instance.CreateTaskDataInstance(taskTemplateType, cmTaskInstance, featureDepth);
            }

            // Recalculate the current system state again after the feature has tasks assigned
            featureDto.RecalculateSystemState();

            return featureDto;
        }
    }
}
