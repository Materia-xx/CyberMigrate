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
            taskTypes.Add(nameof(FeatureDependencyTask));
            taskTypes.Add(nameof(NoteTask));
            return taskTypes;
        }

        public override List<string> GetRequiredTaskStates(CMTaskTypeDto cmTaskType)
        {
            var requiredStates = new List<string>();

            switch (cmTaskType.Name)
            {
                case nameof(FeatureDependencyTask):
                    requiredStates.Add("WaitingOnDependency");
                    break;
                case nameof(NoteTask):
                    // No required states in the note task
                    break;
            }

            return requiredStates;
        }

        public override CMTaskBase GetTask(CMTaskTypeDto cmTaskType, CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            switch (cmTaskType.Name)
            {
                case nameof(FeatureDependencyTask):
                    var featureTask = new FeatureDependencyTask();
                    featureTask.CmSystemId = cmSystem.Id;
                    featureTask.CmFeatureId = cmFeature.Id;
                    featureTask.CmTaskId = cmTask.Id;
                    return featureTask;
                case nameof(NoteTask):
                    var noteTask = new NoteTask();
                    noteTask.CmSystemId = cmSystem.Id;
                    noteTask.CmFeatureId = cmFeature.Id;
                    noteTask.CmTaskId = cmTask.Id;
                    return noteTask;
            }

            return null;
        }

        public override UserControl GetTaskConfigUI(CMTaskTypeDto cmTaskType)
        {
            switch (cmTaskType.Name)
            {
                case nameof(FeatureDependencyTask):
                    var configUI = new FeatureDependencyConfigUC();
                    return configUI;
                case nameof(NoteTask):
                    // No config UI for the note task
                    return null;
            }

            return null;
        }

        public override UserControl GetTaskUI(CMTaskTypeDto cmTaskType, CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            switch (cmTaskType.Name)
            {
                case nameof(FeatureDependencyTask):
                    var featureDependencyTaskUI = new FeatureDependencyUC(cmSystem, cmFeature, cmTask);
                    return featureDependencyTaskUI;
                case nameof(NoteTask):
                    var noteTaskUI = new NoteUC(cmSystem, cmFeature, cmTask);
                    return noteTaskUI;
            }

            return null;
        }

        public override void CreateTaskDataInstance(CMTaskTypeDto cmTaskType, CMTaskDto cmTaskInstance, int featureDepth)
        {
            switch (cmTaskType.Name)
            {
                case nameof(FeatureDependencyTask):
                    FeatureDependency_CreateTaskDataInstance(cmTaskInstance, featureDepth);
                    break;
                case nameof(NoteTask):
                    Note_CreateTaskDataInstance(cmTaskInstance, featureDepth);
                    break;
            }
        }

        private void FeatureDependency_CreateTaskDataInstance(CMTaskDto cmTaskInstance, int featureDepth)
        {
            // The new task Dto instance that was created is passed in

            // The task Dto template that the above task Dto instance was created from
            var taskDtoTemplate = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmTaskInstance.CMParentTaskTemplateId);

            // The task data for the dependency that was defined for the Dto template
            var taskDataTemplate = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(taskDtoTemplate.Id);

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
        }

        private void Note_CreateTaskDataInstance(CMTaskDto cmTaskInstance, int featureDepth)
        {
            // The new task Dto instance that was created is passed in

            // The task Dto template that the above task Dto instance was created from
            var taskDtoTemplate = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmTaskInstance.CMParentTaskTemplateId);
            // mcbtodo: this seems like it will be a common pattern to also get the task template, so pass that in by default

            // The task data note template that we'll clone
            var taskDataTemplate = BuildInTasksDataProviders.NoteDataProvider.Get_ForTaskId(taskDtoTemplate.Id);

            // If there was no task data template defined then just return without creating data for the instance
            if (taskDataTemplate == null)
            {
                return;
            }

            // Now we can create new task data note
            var taskData = new NoteDto()
            {
                Note = taskDataTemplate.Note // mcbtodo: apply feature vars here when they are available
            };

            var opResult = BuildInTasksDataProviders.NoteDataProvider.Insert(taskData);
            if (opResult.Errors.Any())
            {
                throw new Exception(opResult.ErrorsCombined);
            }
        }

        public override void RegisterCMCUDCallbacks()
        {
            CMDataProvider.DataStore.Value.CMTasks.Value.OnCUD += OnTaskCUD;
        }

        private void OnTaskCUD(CMCUDEventArgs cmCUDEventArgs)
        {
            // We're only interested in delete operations here
            if (cmCUDEventArgs.ActionType != CMCUDActionType.Delete)
            {
                return;
            }

            // Try to get a ref to the task being affected
            var cmTask = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmCUDEventArgs.Id);
            if (cmTask == null)
            {
                return;
            }

            // Try to figure out what the task type is
            var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(cmTask.CMTaskTypeId); // mcbtodo: create a function to try and get this directly from the task id in the tasktype crud provider
            if (cmTaskType == null)
            {
                return;
            }

            switch (cmTaskType.Name)
            {
                case nameof(FeatureDependencyTask):
                    BuildInTasksDataProviders.FeatureDependencyDataProvider.Delete_ForTaskId(cmCUDEventArgs.Id);
                    break;
                case nameof(NoteTask):
                    BuildInTasksDataProviders.NoteDataProvider.Delete_ForTaskId(cmCUDEventArgs.Id);
                    break;
            }
        }
    }
}
