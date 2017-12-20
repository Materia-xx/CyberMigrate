using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using TaskBase;

namespace Tasks.BuiltIn
{
    [Export(typeof(CMTaskFactoryBase))]
    public class FeatureDependencyTaskFactory : CMTaskFactoryBase
    {
        public FeatureDependencyTaskFactory()
        {
            SupportedTasks.Add(nameof(FeatureDependencyTask));
        }

        public override CMTaskBase CreateTask(int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            var featureTask = new FeatureDependencyTask();
            featureTask.CmSystemId = cmSystemId;
            featureTask.CmFeatureId = cmFeatureId;
            featureTask.CmTaskId = cmTaskId;
            return featureTask;
        }

        public override UserControl GetConfigurationUI()
        {
            var configUI = new FeatureDependencyTaskFactoryUC();
            return configUI;
        }
    }
}
