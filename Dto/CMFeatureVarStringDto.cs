namespace Dto
{
    public class CMFeatureVarStringDto : IdBasedObject
    {
        /// <summary>
        /// The feature id that this feature var is attached to
        /// </summary>
        public int CMFeatureId { get; set; }

        /// <summary>
        /// The name of the feature template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the feature var
        /// </summary>
        public string Value { get; set; }
    }
}
