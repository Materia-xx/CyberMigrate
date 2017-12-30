using DataProvider;
using DataProvider.Events;
using Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using TaskBase;
using TaskBase.Extensions;
using Tasks.BuiltIn.FeatureDependency;
using Tasks.BuiltIn.Note;

namespace Tasks.BuiltIn
{
    [Export(typeof(CMTaskFactoryBase))]
    public class CMBuiltInTaskFactory : CMTaskFactoryBase
    {
        private List<CMTaskStateDto> FeatureDependency_TaskStates { get; set; }
        private CMTaskStateDto FeatureDependency_TaskState_WaitingOnDependency { get; set; }
        private CMTaskStateDto FeatureDependency_TaskState_Closed { get; set; }

        public override string Name
        {
            get
            {
                return nameof(CMBuiltInTaskFactory);
            }
        }

        public override List<string> GetTaskTypes()
        {
            var taskTypes = new List<string>();
            taskTypes.Add(nameof(BuildInTaskTypes.FeatureDependency));
            taskTypes.Add(nameof(BuildInTaskTypes.Note));
            return taskTypes;
        }

        public override List<string> GetRequiredTaskStates(CMTaskTypeDto cmTaskType)
        {
            var requiredStates = new List<string>();

            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    requiredStates.Add("WaitingOnDependency");
                    break;
                case nameof(BuildInTaskTypes.Note):
                    // No required states in the note task
                    break;
            }

            return requiredStates;
        }

        public override UserControl GetTaskConfigUI(CMTaskTypeDto cmTaskType)
        {
            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    var configUI = new FeatureDependencyConfigUC();
                    return configUI;
                case nameof(BuildInTaskTypes.Note):
                    // No config UI for the note task
                    return null;
            }

            return null;
        }

        public override UserControl GetTaskUI(CMTaskTypeDto cmTaskType, CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    var featureDependencyTaskUI = new FeatureDependencyUC(cmSystem, cmFeature, cmTask);
                    return featureDependencyTaskUI;
                case nameof(BuildInTaskTypes.Note):
                    var noteTaskUI = new NoteUC(cmSystem, cmFeature, cmTask);
                    return noteTaskUI;
            }

            return null;
        }

        public override void CreateTaskDataInstance(CMTaskTypeDto cmTaskType, CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance, int featureDepth)
        {
            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    FeatureDependency_CreateTaskDataInstance(cmTaskTemplate, cmTaskInstance, featureDepth);
                    break;
                case nameof(BuildInTaskTypes.Note):
                    Note_CreateTaskDataInstance(cmTaskTemplate, cmTaskInstance, featureDepth);
                    break;
            }
        }

        private void FeatureDependency_CreateTaskDataInstance(CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance, int featureDepth)
        {
            // The task data (template) to clone
            var taskDataTemplate = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(cmTaskTemplate.Id);

            // If there was no task data template defined then just return without creating data for the instance
            if (taskDataTemplate == null)
            {
                return;
            }

            // The feature template that the dependency template is pointing at
            var featureTemplate = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(taskDataTemplate.CMFeatureId);

            // Clone the target feature template into a new feature instance so the cloned dependency can point at a real instance also.
            // At this point each dependency found will create a new instance of the target feature
            // I had the idea of adding more functionality and allowing several dependency tasks to point at different states of the
            // same feature. But it's a v2 thing.
            var clonedFeatureInstance = featureTemplate.CreateFeatureInstance(featureDepth);

            // Now we can create new task data that points at the new feature instance
            var taskData = new FeatureDependencyDto()
            {
                CMFeatureId = clonedFeatureInstance.Id,
                CMTargetSystemStateId = taskDataTemplate.CMTargetSystemStateId,
                TaskId = cmTaskInstance.Id
            };

            var opResult = BuildInTasksDataProviders.FeatureDependencyDataProvider.Insert(taskData);
            if (opResult.Errors.Any())
            {
                throw new Exception(opResult.ErrorsCombined);
            }

            UpdateTaskStatesForFeatureDependendies(clonedFeatureInstance, clonedFeatureInstance);
        }

        private void Note_CreateTaskDataInstance(CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance, int featureDepth)
        {
            // The task data (template) to clone
            var taskDataTemplate = BuildInTasksDataProviders.NoteDataProvider.Get_ForTaskId(cmTaskTemplate.Id);

            // If there was no task data template defined then just return without creating data for the instance
            if (taskDataTemplate == null)
            {
                return;
            }

            // Now we can create new task data note
            var taskData = new NoteDto()
            {
                TaskId = cmTaskInstance.Id,
                Note = taskDataTemplate.Note // mcbtodo: apply feature vars here when they are available
            };

            var opResult = BuildInTasksDataProviders.NoteDataProvider.Insert(taskData);
            if (opResult.Errors.Any())
            {
                throw new Exception(opResult.ErrorsCombined);
            }
        }

        public override void Initialize()
        {
            var featureDependencyTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(nameof(BuildInTaskTypes.FeatureDependency));
            FeatureDependency_TaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(featureDependencyTaskType.Id).ToList();
            FeatureDependency_TaskState_WaitingOnDependency = FeatureDependency_TaskStates.First(s => s.InternalName.Equals("WaitingOnDependency")); // mcbtodo: const this or something
            FeatureDependency_TaskState_Closed = FeatureDependency_TaskStates.First(s => s.InternalName.Equals(ReservedTaskStates.Closed));

            // mcbtodo: figure out why typing += <tab><tab> here doesn't do anything, but yet the events are working fine.
            CMDataProvider.DataStore.Value.CMTasks.Value.OnRecordDeleted += Task_Deleted;

            // If a feature is somehow deleted then any feature dependency that was pointing at it can be resolved
            // If a feature state is changed to the one being monitored for then it can be resolved
            // If a feature is inserted and a dependency was already watching that feature id ... no, that doesn't make sense.
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordDeleted += Feature_Deleted;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Feature_Updated;
        }

        private void Task_Deleted(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs)
        {
            var cmTaskDto = deletedRecordEventArgs.DtoBefore as CMTaskDto;
            // Try to figure out what the task type is
            var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForTaskId(cmTaskDto.Id);
            if (cmTaskType == null)
            {
                return;
            }

            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    BuildInTasksDataProviders.FeatureDependencyDataProvider.Delete_ForTaskId(cmTaskDto.Id);
                    break;
                case nameof(BuildInTaskTypes.Note):
                    BuildInTasksDataProviders.NoteDataProvider.Delete_ForTaskId(cmTaskDto.Id);
                    break;
            }
        }

        private void Feature_Deleted(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs)
        {
            var beforeDto = deletedRecordEventArgs.DtoBefore as CMFeatureDto;

            UpdateTaskStatesForFeatureDependendies(beforeDto, null);
        }

        private void Feature_Updated(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            // Figure out if the feature state was updated, which is what we're really interested in here.
            var beforeDto = updatedRecordEventArgs.DtoBefore as CMFeatureDto;
            var afterDto = updatedRecordEventArgs.DtoAfter as CMFeatureDto;

            // Currently updates to feature templates system status doesn't happen, but if it starts at some point, just go with it until it becomes and issue

            // If the feature system state was updated
            if (beforeDto.CMSystemStateId != afterDto.CMSystemStateId)
            {
                UpdateTaskStatesForFeatureDependendies(beforeDto, afterDto);
            }
        }

        private void UpdateTaskStatesForFeatureDependendies(CMFeatureDto featureBeforeDto, CMFeatureDto featureAfterDto)
        {
            // Look for dependency task data that reference this feature that is being changed
            var linkedTaskDatas = BuildInTasksDataProviders.FeatureDependencyDataProvider.GetAll();
            // mcbtodo: Is there a way to code in CRUD provider overrides for the FeatureDependencyDataProvider so this doesn't have to do a GetAll() ?
            linkedTaskDatas = linkedTaskDatas.Where(t => t.CMFeatureId == featureBeforeDto.Id);

            // If there are no dependencies on this feature, then there is nothing more to do.
            if (!linkedTaskDatas.Any())
            {
                return;
            }

            // mcbtodo: still has some kinks to work out because when instancing from a template that has a dep task in it, the dep task ends in in the "instance" state after creation instead of the "WaitingForDependency" state

            // For each dependency data that was watching this feature
            foreach (var linkedTaskData in linkedTaskDatas)
            {
                var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(linkedTaskData.TaskId);
                if (cmTask == null)
                {
                    // If the task this data links to is null then somehow the task was deleted without deleting the data,
                    // Do so now and return
                    BuildInTasksDataProviders.FeatureDependencyDataProvider.Delete(linkedTaskData.Id);
                    return;
                }

                // Figure out what the task (that is associated with the dependency data) state should be
                var shouldBeState = featureAfterDto == null || linkedTaskData.CMTargetSystemStateId == featureAfterDto.CMSystemStateId ?
                    FeatureDependency_TaskState_Closed :
                    FeatureDependency_TaskState_WaitingOnDependency;

                // Now check to see if the dependency task actually is in that state
                if (cmTask.CMTaskStateId != shouldBeState.Id)
                {
                    // If it's not in the state it should be, then do the update so it is.
                    // All of the checks to avoid doing an update are to avoid chain reactions with the CUD events
                    cmTask.CMTaskStateId = shouldBeState.Id;
                    CMDataProvider.DataStore.Value.CMTasks.Value.Update(cmTask);
                }
            }
        }
    }
}
