using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tasks.BuiltIn.FeatureDependency
{
    public class FeatureDependencyDto : CMTaskDataDtoBase
    {
        /// <summary>
        /// Indicates if a path option has been chosen.
        /// If set to true and <see cref="InstancedCMFeatureId"/> is zero it means that a choice that led to no feature was chosen.
        /// </summary>
        public bool PathOptionChosen { get; set; }

        /// <summary>
        /// Represents the feature dependency instance that was chosen from PathOptions.
        /// Will be set to 0 if a choice has not yet been made.
        /// </summary>
        public int InstancedCMFeatureId { get; set; }

        /// <summary>
        /// Represents the target system state that the instanced feature must be in for the associated dependency task to be closed
        /// </summary>
        public int InstancedTargetCMSystemStateId { get; set; }

        /// <summary>
        /// The list of potential feature dependencies with associated rules to determine if they will be instanced as the dependency
        /// </summary>
        public List<FeatureDependencyPathOptionDto> PathOptions { get; set; } = new List<FeatureDependencyPathOptionDto>();
    }
}
