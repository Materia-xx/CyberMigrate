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
        /// Creates an instance of the task, setting the instanced task to the <see cref="ReservedTaskStates.Instance"/> state.
        /// The clone task Id is left at 0 and not inserted into the database.
        /// System and feature ids are re-assigned to the passed in values.
        /// All other values are an exact copy from the template with no feature vars resolved.
        /// </summary>
        /// <param name="taskTemplate"></param>
        /// <param name="cmFeatureId">The feature id that the clone should be assigned to</param>
        /// <returns></returns>
        public static CMTaskDto ToInstance(this CMTaskDto taskTemplate, int cmFeatureId)
        {
            // Cloning something that is not a template is not an implemented feature
            if (!taskTemplate.IsTemplate)
            {
                throw new NotImplementedException("Instancing a task that is not a template is not implemented.");
            }

            var taskTemplateType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(taskTemplate.CMTaskTypeId);

            // Instance the task
            var taskInstance = new CMTaskDto()
            {
                CMFeatureId = cmFeatureId,
                CMParentTaskTemplateId = taskTemplate.Id,
                CMSystemStateId = taskTemplate.CMSystemStateId,
                CMTaskStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Instance, taskTemplateType.Id).Id,
                CMTaskTypeId = taskTemplate.CMTaskTypeId,
                Title = taskTemplate.Title
            };

            return taskInstance;
        }

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
        public static CMFeatureDto ToInstance(this CMFeatureDto featureTemplate, int featureDepth, List<CMFeatureVarStringDto> initialFeatureVars)
        {
            if (!featureTemplate.IsTemplate)
            {
                throw new NotImplementedException("Instancing a feature that is already an instance is not implemented.");
            }

            featureDepth++;
            if (featureDepth > 10) // mcbtodo: make this configurable in the master options
            {
                // mcbtodo: this will leave a mess of features and records that needs to be cleaned up, make that easier to deal with.
                throw new StackOverflowException("Unable to clone a feature template that is more than 10 levels deep. Check the feature for errors and try again.");
            }

            // Clone the feature template to a feature instance
            var featureInstanceDto = new CMFeatureDto()
            {
                CMParentFeatureTemplateId = featureTemplate.Id,
                CMSystemId = featureTemplate.CMSystemId,
                Name = featureTemplate.Name
            };

            // Calculate a default feature state, which is needed to insert into the db
            // Due to the fact that the feature has no cloned tasks yet, this state is likely temporary and will be updated again below
            featureInstanceDto.RecalculateSystemState();

            var opFeatureInsert = CMDataProvider.DataStore.Value.CMFeatures.Value.Insert(featureInstanceDto);
            if (opFeatureInsert.Errors.Any())
            {
                throw new Exception(opFeatureInsert.ErrorsCombined);
            }

            // The new feature has an Id now, we can set up the feature vars specific to this feature
            initialFeatureVars.Add(
                new CMFeatureVarStringDto()
                {
                    Name = "Feature.Id",
                    Value = featureInstanceDto.Id.ToString()
                });

            // Add all feature vars to the feature
            // This will trigger other routines that resolve the feature vars within tasks and features to execute in response through the CUD events
            foreach (var featureVar in initialFeatureVars)
            {
                // Attach the feature var to the new feature
                featureVar.CMFeatureId = featureInstanceDto.Id;

                var opFeatureVarStringInsert = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.Insert(featureVar);
                if (opFeatureVarStringInsert.Errors.Any())
                {
                    throw new Exception(opFeatureVarStringInsert.ErrorsCombined);
                }
            }

            // Instances each task in the feature template
            var taskTemplates = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(featureTemplate.Id, true);
            foreach (var cmTaskTemplate in taskTemplates)
            {
                var taskInstance = cmTaskTemplate.ToInstance(featureInstanceDto.Id);

                var opTaskInsert = CMDataProvider.DataStore.Value.CMTasks.Value.Insert(taskInstance);
                if (opTaskInsert.Errors.Any())
                {
                    throw new Exception(opTaskInsert.ErrorsCombined);
                }

                // For the task data we revert to the task factory to provide it
                var taskTemplateType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(cmTaskTemplate.CMTaskTypeId);
                TaskFactoriesCatalog.Instance.CreateTaskDataInstance(taskTemplateType, cmTaskTemplate, taskInstance, featureDepth);
            }

            // Recalculate the current system state again after the feature has tasks assigned
            featureInstanceDto.RecalculateSystemState();

            return featureInstanceDto;
        }
    }
}
