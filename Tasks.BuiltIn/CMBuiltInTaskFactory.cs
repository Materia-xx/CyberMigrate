using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using TaskBase;
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
    }
}
