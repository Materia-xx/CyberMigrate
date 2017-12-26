using System;
using System.Windows.Controls;
using TaskBase;

namespace Tasks.BuiltIn.FeatureDependency
{
    public class FeatureDependencyTask : CMTaskBase
    {
        public override void AutoProgress()
        {
            // mcbtodo:
            throw new NotImplementedException();
        }

        public override UserControl GetUI() // mcbtodo: remove, don't need this for raw task processing, the factory can give it when needed
        {
            // mcbtodo:
            throw new NotImplementedException();
        }
    }
}
