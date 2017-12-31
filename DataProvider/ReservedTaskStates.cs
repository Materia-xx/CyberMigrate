using System.Collections.Generic;

namespace DataProvider
{
    public static class ReservedTaskStates
    {
        /// <summary>
        /// A <see cref="Closed"/> item is considered closed by the program and enables features to progress to their next system state.
        /// </summary>
        public const string Closed = "Closed";

        /// <summary>
        /// A task in the <see cref="Template"/> state is part of the configuration.
        /// A task in this state is always cloned to another copy before it can become an active task that is tracking a real task.
        /// </summary>
        public const string Template = "Template";

        /// <summary>
        /// A task in the <see cref="Instance"/> state is a copy of a task from the <see cref="Template"/> state and is free to have feature vars resolved at any point.
        /// </summary>
        public const string Instance = "Instance";

        public static List<string> States { get; set; } = new List<string>()
        {
            Template,
            Instance,
            Closed
        };
    }
}
