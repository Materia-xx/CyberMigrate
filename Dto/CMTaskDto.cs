namespace Dto
{
    public class CMTaskDto : IdBasedObject
    {
        /// <summary>
        /// The feature that this task is connected to
        /// </summary>
        public int CMFeatureId { get; set; }

        /// <summary>
        /// The task type of this feature
        /// </summary>
        public int CMTaskTypeId { get; set; }

        /// <summary>
        /// The parent task template id that this task was created from (If it is a task instance)
        /// 0 If this is a task template
        /// </summary>
        public int CMParentTaskTemplateId { get; set; }

        /// <summary>
        /// Indicates if this is a task template or an actual implementation of a task
        /// </summary>
        public bool IsTemplate
        {
            get
            {
                return CMParentTaskTemplateId == 0;
            }
        }

        /// <summary>
        /// The system state that this task exists under
        /// </summary>
        public int CMSystemStateId { get; set; }

        /// <summary>
        /// The task state that the task is currently in
        /// </summary>
        public int CMTaskStateId { get; set; }

        /// <summary>
        /// The title of the task
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The order that the task should be executed in, relative to other tasks in the same feature and system state.
        /// </summary>
        public int ExecutionOrder { get; set; }
    }
}
