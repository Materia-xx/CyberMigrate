namespace Dto
{
    public class CMTaskStateDto : IdBasedObject
    {
        /// <summary>
        /// The task type that this state exists under
        /// </summary>
        public int TaskTypeId { get; set; }

        /// <summary>
        /// How the task plugin referrs to the state. Not updateable via user.
        /// Also used to refer to reserved states such as "Complete", "Template" and "Instance"
        /// </summary>
        public string PluginTaskStateName { get; set; }

        /// <summary>
        /// How the task is displayed in the program. This name can be changed by the user.
        /// </summary>
        public string DisplayTaskStateName { get; set; }
    }
}
