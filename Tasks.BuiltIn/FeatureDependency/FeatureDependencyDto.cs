using Dto;

namespace Tasks.BuiltIn.FeatureDependency
{
    public class FeatureDependencyDto : CMTaskDataDtoBase
    {
        /// <summary>
        /// The feature that is being tracked
        /// </summary>
        public int CMFeatureId { get; set; }

        /// <summary>
        /// The target state that will trigger the dependency
        /// </summary>
        public int CMTargetSystemStateId { get; set; }
    }
}
