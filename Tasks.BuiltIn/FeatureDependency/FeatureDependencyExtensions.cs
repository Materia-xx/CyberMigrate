using DataProvider;
using DataProvider.Events;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskBase.Extensions;

namespace Tasks.BuiltIn.FeatureDependency
{
    public static class FeatureDependencyExtensions
    {
        internal static FeatureDependencyTaskDataCRUD FeatureDependencyDataProvider
        {
            get
            {
                if (featureDependencyDataProvider == null)
                {
                    featureDependencyDataProvider = new FeatureDependencyTaskDataCRUD(nameof(FeatureDependencyDto));
                }
                return featureDependencyDataProvider;
            }
        }
        private static FeatureDependencyTaskDataCRUD featureDependencyDataProvider;

        internal static List<CMTaskStateDto> FeatureDependency_TaskStates { get; set; }
        internal static CMTaskStateDto FeatureDependency_TaskState_WaitingOnChoice { get; set; }
        internal static CMTaskStateDto FeatureDependency_TaskState_WaitingOnDependency { get; set; }
        internal static CMTaskStateDto FeatureDependency_TaskState_Closed { get; set; }

        /// <summary>
        /// Creates an instance of the task data template
        /// </summary>
        /// <returns></returns>
        internal static FeatureDependencyDto ToInstance(this FeatureDependencyDto taskDataTemplate, int taskInstanceId)
        {
            // Cloning something that is not a task data template is not an implemented feature
            if (taskDataTemplate.InstancedCMFeatureId != 0)
            {
                throw new NotImplementedException("Instancing task data from another task data instance is not implemented.");
            }

            // Instance the task data
            var taskDataInstance = new FeatureDependencyDto()
            {
                TaskId = taskInstanceId
            };

            // Copy the path options over
            foreach (var pathOption in taskDataTemplate.PathOptions)
            {
                var clonedPathOption = new FeatureDependencyPathOptionDto()
                {
                    CMFeatureTemplateId = pathOption.CMFeatureTemplateId,
                    CMTargetSystemStateId = pathOption.CMTargetSystemStateId,
                    FeatureVarName = pathOption.FeatureVarName,
                    FeatureVarSetTo = pathOption.FeatureVarSetTo,
                };
                taskDataInstance.PathOptions.Add(clonedPathOption);
            }

            return taskDataInstance;
        }

        internal static void FeatureDependencyData_Created(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs)
        {
            // The TaskData that was created
            var taskData = createdRecordEventArgs.CreatedDto as FeatureDependencyDto;

            // The task that this TaskData is associated with
            var task = CMDataProvider.DataStore.Value.CMTasks.Value.Get(taskData.TaskId);

            // Get all feature vars that currently exist for the feature that the task is in
            var featureVars = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(task.CMFeatureId).ToList();

            // When a new feature dependency task data shows up we want to look through all of its Path options to see if a feature can be instanced right away
            FeatureDependency_ResolveFeatureVars(task, featureVars);

            UpdateTaskStatesForFeatureDependendies(taskData, null);
        }

        internal static void FeatureDependencyData_Updated(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            // The TaskData in its updated state
            var taskData = updatedRecordEventArgs.DtoAfter as FeatureDependencyDto;

            // The task that this TaskData is associated with
            var task = CMDataProvider.DataStore.Value.CMTasks.Value.Get(taskData.TaskId);

            // Get all feature vars that currently exist for the feature that the task is in
            var featureVars = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(task.CMFeatureId).ToList();

            // An update to the task data may include changed path options that make it so we can now instance a dependant feature, so look for that now.
            FeatureDependency_ResolveFeatureVars(task, featureVars);

            UpdateTaskStatesForFeatureDependendies(taskData, null);
        }

        internal static void FeatureDependency_CreateTaskDataInstance(CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance)
        {
            // Double check to make sure there isn't task data for this task already
            var existingTaskData = FeatureDependencyDataProvider.Get_ForTaskId(cmTaskInstance.Id);
            if (existingTaskData != null)
            {
                throw new Exception("Task data already exists for the feature dependency task.");
            }

            // The task data (template) to clone
            var taskDataTemplate = FeatureDependencyDataProvider.Get_ForTaskId(cmTaskTemplate.Id);

            // If there was no task data template defined then just return without creating data for the instance
            if (taskDataTemplate == null)
            {
                return;
            }

            // Clone the template task data
            var taskDataInstance = taskDataTemplate.ToInstance(cmTaskInstance.Id);

            // And insert it
            var opInsertResult = FeatureDependencyExtensions.FeatureDependencyDataProvider.Insert(taskDataInstance);
            if (opInsertResult.Errors.Any())
            {
                throw new Exception(opInsertResult.ErrorsCombined);
            }

            // A new task data has just been inserted, CUD triggers that react to the taskdata created should take over from here.
        }

        /// <summary>
        /// Call this when a feature is updated, it will determine if any dependency should be moved to the closed state.
        /// Also moves out of the closed state if the dependant feature moves away from its target state.
        /// </summary>
        /// <param name="featureBeforeDto"></param>
        /// <param name="featureAfterDto"></param>
        internal static void UpdateTaskStatesForFeatureDependendies(CMFeatureDto featureBeforeDto, CMFeatureDto featureAfterDto)
        {
            // FeatureAfterDto may be null during a delete, use the before version for the Id and template check, later use the after one if it is not null

            // Don't process updates if the feature isn't real yet.
            if (featureBeforeDto.Id == 0)
            {
                return;
            }

            // Don't process updates for feature templates
            if (featureBeforeDto.IsTemplate)
            {
                return;
            }

            // Look for dependency task data that reference this feature that is being changed
            var linkedTaskDatas = FeatureDependencyDataProvider.GetAll_ForInstancedFeatureId(featureBeforeDto.Id);

            // If there are no dependencies on the feature that was updated, then there is nothing more to do.
            if (!linkedTaskDatas.Any())
            {
                return;
            }

            // For each dependency data that was watching this feature
            foreach (var linkedTaskData in linkedTaskDatas)
            {
                UpdateTaskStatesForFeatureDependendies(linkedTaskData, featureAfterDto);
            }
        }

        internal static void UpdateTaskStatesForFeatureDependendies(FeatureDependencyDto linkedTaskData, CMFeatureDto trackedFeature)
        {
            var cmTaskInstance = CMDataProvider.DataStore.Value.CMTasks.Value.Get(linkedTaskData.TaskId);
            if (cmTaskInstance == null)
            {
                // If the task this data links to is null then somehow the task was deleted without deleting the data,
                // Do so now
                FeatureDependencyDataProvider.Delete(linkedTaskData.Id);
                return;
            }

            if (trackedFeature == null && linkedTaskData.InstancedCMFeatureId != 0)
            {
                trackedFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(linkedTaskData.InstancedCMFeatureId);
            }

            // Figure out what the task (that is associated with the dependency data) state should be
            CMTaskStateDto shouldBeState = null;
            if (linkedTaskData.InstancedCMFeatureId == 0)
            {
                shouldBeState = FeatureDependency_TaskState_WaitingOnChoice;
            }
            else
            {
                shouldBeState = trackedFeature == null || linkedTaskData.InstancedTargetCMSystemStateId == trackedFeature.CMSystemStateId ?
                    FeatureDependency_TaskState_Closed :
                    FeatureDependency_TaskState_WaitingOnDependency;
            }

            // Now check to see if the dependency task actually is in that state
            if (cmTaskInstance.CMTaskStateId != shouldBeState.Id)
            {
                // If it's not in the state it should be, then do the update so it is.
                // All of the checks to avoid doing an update are to avoid chain reactions with the CUD events
                cmTaskInstance.CMTaskStateId = shouldBeState.Id;
                CMDataProvider.DataStore.Value.CMTasks.Value.Update(cmTaskInstance);
            }
        }

        /// <summary>
        /// Examines the current set of path options for the feature dependency task data and creates a feature instance if one of the path options matches the rules.
        /// Only 1 feature will be instanced. If a feature has already been instanced then this function will do nothing.
        /// If no path option rules match, then the function does nothing.
        /// </summary>
        internal static void FeatureDependency_ResolveFeatureVars(CMTaskDto taskInstance, List<CMFeatureVarStringDto> featureVars)
        {
            FeatureDependencyDto instanceTaskData = FeatureDependencyDataProvider.Get_ForTaskId(taskInstance.Id);

            if (instanceTaskData.InstancedCMFeatureId > 0)
            {
                // There is already a feature instance created and being tracked
                return;
            }

            // The feature instance that the task is part of
            var featureInstance = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(taskInstance.CMFeatureId);

            // Do not process if we are dealing with a template
            if (taskInstance.IsTemplate || featureInstance.IsTemplate)
            {
                return;
            }

            // Examine all path options
            foreach (var pathOption in instanceTaskData.PathOptions.OrderBy(po => po.Order))
            {
                bool useThisOption = false;

                // A setting with a blank feature var name is auto-chosen if we get to it
                if (string.IsNullOrWhiteSpace(pathOption.FeatureVarName))
                {
                    useThisOption = true;
                }
                else
                {
                    // If the feature var to look for is not yet present in the featurevars collection, then we don't do anything
                    var matchingFeatureVar = featureVars.FirstOrDefault(v => v.Name.Equals(pathOption.FeatureVarName, StringComparison.OrdinalIgnoreCase));
                    if (matchingFeatureVar != null && matchingFeatureVar.Value.Equals(pathOption.FeatureVarSetTo, StringComparison.OrdinalIgnoreCase))
                    {
                        useThisOption = true;
                    }
                }

                if (useThisOption)
                {
                    // The feature template referred to by this option
                    var featureTemplate = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(pathOption.CMFeatureTemplateId);

                    var childFeatureVars = new List<CMFeatureVarStringDto>();
                    var clonedFeatureInstance = featureTemplate.ToInstance(childFeatureVars);

                    // Set the task data to represent the feature instance that was created
                    instanceTaskData.InstancedCMFeatureId = clonedFeatureInstance.Id;
                    instanceTaskData.InstancedTargetCMSystemStateId = pathOption.CMTargetSystemStateId;
                    var opUpdateTaskData = FeatureDependencyDataProvider.Update(instanceTaskData);
                    if (opUpdateTaskData.Errors.Any())
                    {
                        throw new Exception(opUpdateTaskData.ErrorsCombined);
                    }

                    break;
                }
            }
        }
    }
}
