namespace Dto
{
    public class CMFeatureDto : IdBasedObject
    {
        /// <summary>
        /// The sytem that the feature is connected to
        /// </summary>
        public int CMSystemId { get; set; }

        /// <summary>
        /// The parent feature template that this feature was created from (if this is an instanced feature)
        /// Otherwise this will be set to 0 if it is a feature template
        /// </summary>
        public int CMParentFeatureTemplateId { get; set; }

        /// <summary>
        /// Indicates if this is a feature template or an actual implementation of a feature
        /// </summary>
        public bool IsTemplate { get; set; }

        /// <summary>
        /// The name of the feature template
        /// </summary>
        public string Name { get; set; }
    }
}
