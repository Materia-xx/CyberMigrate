using DataProvider;
using Tasks.BuiltIn.FeatureDependency;

namespace Tasks.BuiltIn
{
    internal static class BuildInTasksDataProviders
    {
        internal static CMTaskDataCRUD<FeatureDependencyDto> FeatureDependencyDataProvider
        {
            get
            {
                if (featureDependencyDataProvider == null)
                {
                    featureDependencyDataProvider = CMDataProvider.GetTaskTypeDataProvider<FeatureDependencyDto>();
                }
                return featureDependencyDataProvider;
            }
        }
        private static CMTaskDataCRUD<FeatureDependencyDto> featureDependencyDataProvider;
    }
}
