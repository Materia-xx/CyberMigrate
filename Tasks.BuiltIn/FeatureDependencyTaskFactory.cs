using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using TaskBase;

namespace Tasks.BuiltIn
{
    [Export(typeof(CMTaskFactoryBase))]
    public class FeatureDependencyTaskFactory : CMTaskFactoryBase
    {
        public override string Name
        {
            get
            {
                return nameof(FeatureDependencyTaskFactory);
            }
        }

        public override CMTaskBase CreateTask(int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            var featureTask = new FeatureDependencyTask();
            featureTask.CmSystemId = cmSystemId;
            featureTask.CmFeatureId = cmFeatureId;
            featureTask.CmTaskId = cmTaskId;
            return featureTask;
        }

        public override List<string> GetTaskTypes()
        {
            var taskTypes = new List<string>();
            taskTypes.Add(nameof(FeatureDependencyTask));
            return taskTypes;
        }

        public override UserControl GetTaskTypeConfigUI(string taskTypeName)
        {
            switch (taskTypeName)
            {
                case nameof(FeatureDependencyTask):
                    var configUI = new FeatureDependencyConfigUC();
                    return configUI;
            }

            return null;
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
    }
}
