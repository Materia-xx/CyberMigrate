namespace Dto
{
    public class CMFeatureStateTransitionRule : IdBasedObject
    {
        /// <summary>
        /// Transition rules are processed in order with the first one that matches winning and the rest being ignored
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The feature template that this rule is for
        /// </summary>
        public int CMFeatureTemplateId { get; set; }

        /// <summary>
        /// The system state id that will be moved to if the rule passes
        /// </summary>
        public int ToCMSystemStateId { get; set; }

        /// <summary>
        /// If true then all tasks are examined. If false then only 1 task needs to meed the conditions to satisfy the rule.
        /// </summary>
        public bool ConditionAllTasks { get; set; }

        /// <summary>
        /// The tasks in this state are examined
        /// </summary>
        public int ConditionQuerySystemStateId { get; set; }

        /// <summary>
        /// If true, then the rule looks for task(s) that are complete. Otherwise it looks for tasks that are not complete.
        /// </summary>
        public bool ConditionTaskComplete { get; set; }
    }
}
