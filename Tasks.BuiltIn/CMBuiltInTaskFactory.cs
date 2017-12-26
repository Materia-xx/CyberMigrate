using DataProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using TaskBase;
using TaskBase.Extensions;
using Tasks.BuiltIn.FeatureDependency;

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
            return taskTypes;
        }

        public override List<string> GetRequiredTaskStates(string taskTypeName)
        {
            var requiredStates = new List<string>();

            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    requiredStates.Add("WaitingOnDependency");
                    break;
            }

            return requiredStates;
        }

        public override CMTaskBase GetTask(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    var featureTask = new FeatureDependencyTask();
                    featureTask.CmSystemId = cmSystemId;
                    featureTask.CmFeatureId = cmFeatureId;
                    featureTask.CmTaskId = cmTaskId;
                    return featureTask;
            }

            return null;
        }

        public override UserControl GetTaskConfigUI(string taskTypeName)
        {
            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    var configUI = new FeatureDependencyConfigUC();
                    return configUI;
            }

            return null;
        }

        public override UserControl GetTaskUI(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    var configUI = new FeatureDependencyUC(cmSystemId, cmFeatureId, cmTaskId);
                    return configUI;
            }

            return null;
        }

        public override void CreateTaskDataInstance(string taskTypeName, int cmTaskInstanceId)
        {
            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    FeatureDependency_CreateTaskDataInstance(cmTaskInstanceId);
                    break;
            }
        }

        private void FeatureDependency_CreateTaskDataInstance(int cmTaskInstanceId)
        {
            // The new task Dto instance that was created
            var taskDto = CMDataProvider.DataStore.Value.CMTasks.Value.Get(cmTaskInstanceId);

            // The task Dto template that the above task Dto instance was created from
            var taskDtoTemplate = CMDataProvider.DataStore.Value.CMTasks.Value.Get(taskDto.CMParentTaskTemplateId);

            // The task data for the dependency that was defined for the Dto template
            var taskDataTemplate = BuildInTasksDataProviders.FeatureDependencyDataProvider.Get_ForTaskId(taskDtoTemplate.Id);

            // mcbtodo: if there was no task data template defined then just return without creating data for the instance
            if (taskDataTemplate == null)
            {
                return;
            }

            // The feature template that the dependency template is pointing at
            var featureTemplate = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(taskDataTemplate.CMFeatureId);

            // mcbtodo: there needs to be some sort of recursion/max depth detection to avoid tasks that depend on features in a loop that never
            // mcbtodo: ends and maxes cpu and eventually fills up the disk or something.

            // Clone the target feature template into a new feature instance so the cloned dependency can point at a real instance also.
            // At this point each dependency found will create a new instance of the target feature
            // I had the idea of adding more functionality and allowing several dependency tasks to point at different states of the
            // same feature. But it's a v2 thing.
            var clonedFeatureInstance = featureTemplate.CreateFeatureInstance();

            // Now we can create new task data that points at the new feature instance
            var taskData = new FeatureDependencyDto()
            {
                CMFeatureId = clonedFeatureInstance.Id,
                CMTargetSystemStateId = taskDataTemplate.CMTargetSystemStateId,
                TaskId = cmTaskInstanceId
            };

            var opResult = BuildInTasksDataProviders.FeatureDependencyDataProvider.Insert(taskData);
            if (opResult.Errors.Any())
            {
                throw new Exception(opResult.ErrorsCombined);
            }
        }

        public override void DeleteTaskData(string taskTypeName, int cmTaskId)
        {
            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    BuildInTasksDataProviders.FeatureDependencyDataProvider.Delete_ForTaskId(cmTaskId);
                    break;
            }
        }
    }
}
