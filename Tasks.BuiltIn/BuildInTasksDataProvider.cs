using DataProvider;
using Tasks.BuiltIn.FeatureDependency;
using Tasks.BuiltIn.Note;

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

        internal static CMTaskDataCRUD<NoteDto> NoteDataProvider
        {
            get
            {
                if (noteDataProvider == null)
                {
                    noteDataProvider = CMDataProvider.GetTaskTypeDataProvider<NoteDto>();
                }
                return noteDataProvider;
            }
        }
        private static CMTaskDataCRUD<NoteDto> noteDataProvider;
    }
}
