namespace Dto
{
    public class CMFeatureTemplateDto : IdBasedObject
    {
        /// <summary>
        /// The sytem that the feature template is connected to
        /// </summary>
        public int CMSystemId { get; set; }

        /// <summary>
        /// The name of the feature template
        /// </summary>
        public string Name { get; set; }
    }
}
