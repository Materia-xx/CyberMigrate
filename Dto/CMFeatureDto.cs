namespace Dto
{
    public class CMFeatureDto : IdBasedObject
    {
        /// <summary>
        /// The sytem that the feature is connected to
        /// </summary>
        public int CMSystemId { get; set; }

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
