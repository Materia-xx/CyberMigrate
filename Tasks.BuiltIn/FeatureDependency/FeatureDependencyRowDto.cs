using DataProvider;
using Dto;

namespace Tasks.BuiltIn.FeatureDependency
{
    public class FeatureDependencyRowDto
    {
        /// <summary>
        /// The feature var name to look at to test if this will be a dependency or not.
        /// Leave blank to unconditionally use the dependency
        /// </summary>
        public string FeatureVarName { get; set;  }

        /// <summary>
        /// The value that the feature var must be set to in order for the dependency to path into this connected feature
        /// </summary>
        public string FeatureVarSetTo { get; set; }

        /// <summary>
        /// The feature template to instance if this path is selected
        /// </summary>
        public int CMFeatureTemplateId
        {
            get
            {
                return cmFeatureTemplateId;
            }
            set
            {
                cmFeatureTemplateId = value;

                if (value == 0)
                {
                    SetFeatureButtonText = "<Set Feature Template>";
                }
                else
                {
                    var cmFeature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(value);
                    SetFeatureButtonText = cmFeature.Name;
                }
            }
        }
        private int cmFeatureTemplateId;

        /// <summary>
        /// The target state that will trigger the dependency to mark itself as complete
        /// </summary>
        public int CMTargetSystemStateId { get; set; }

        /// <summary>
        /// Helper property that keeps track of the feature name that this record is pointing at for easy display in the grid
        /// </summary>
        public string SetFeatureButtonText { get; set; } = "<Set Feature Template>";
    }
}
