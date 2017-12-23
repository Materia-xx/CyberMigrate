namespace Dto
{
    public class CMTaskStateDto : IdBasedObject
    {
        /// <summary>
        /// The task type that this state exists under
        /// </summary>
        public int TaskTypeId { get; set; }

        /// <summary>
        /// Helps sort tasks so that tasks in more important states are listed first
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Indicates if the <see cref="InternalName"/> will be renamed to always match the <see cref="DisplayName"/>.
        /// Reserved states cannot be deleted.
        /// </summary>
        public bool Reserved { get; set; }

        /// <summary>
        /// When a call to change the task state is made it should be done using this name.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// How the task is displayed in the program. This name can be changed by the user.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
