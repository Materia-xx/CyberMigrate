namespace Dto
{
    public class CMFeatureStateTransitionRuleDto : IdBasedObject
    {
        /// <summary>
        /// Transition rules are processed in order with the first one that matches winning and the rest being ignored
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The feature (template) that this rule is for
        /// </summary>
        public int CMFeatureId { get; set; }

        /// <summary>
        /// The referenced system state
        /// </summary>
        public int CMSystemStateId { get; set; }
    }
}
