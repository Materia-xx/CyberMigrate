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
        /// Indicates if this is a task template or an actual implementation of a task
        /// </summary>
        public bool IsTemplate { get; set; }

        /// <summary>
        /// The system state that this task exists under
        /// </summary>
        public int CMSystemStateId { get; set; }

        // mcbtodo: add in a way to represent the task state

        /// <summary>
        /// The name/title of the task
        /// </summary>
        public string Name { get; set; }
    }
}
