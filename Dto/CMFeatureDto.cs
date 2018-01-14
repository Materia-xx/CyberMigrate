namespace Dto
{
    public class CMFeatureDto : IdBasedObject
    {
        /// <summary>
        /// The sytem that the feature is connected to
        /// </summary>
        public int CMSystemId { get; set; }

        /// <summary>
        /// The system state that the feature is currently in
        /// </summary>
        public int CMSystemStateId { get; set; }

        /// <summary>
        /// The parent feature template that this feature was created from (if this is an instanced feature)
        /// Otherwise this will be set to 0 if it is a feature template
        /// </summary>
        public int CMParentFeatureTemplateId { get; set; }

        /// <summary>
        /// Indicates if this is a feature template or an actual implementation of a feature
        /// </summary>
        public bool IsTemplate
        {
            get
            {
                return CMParentFeatureTemplateId == 0;
            }
        }

        /// <summary>
        /// The name of the feature template/instance
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of the feature that gives more details than the title.
        /// Not meant for listing tasks though.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The background color for tasks shown for this feature.
        /// Any child features will inherit this color as well.
        /// </summary>
        public string TasksBackgroundColor { get; set; }
    }
}
