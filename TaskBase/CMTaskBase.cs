using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace TaskBase
{
    /// <summary>
    /// All tasks should inherit from this base class in order to be recognized as tasks that can be used by the program
    /// </summary>
    public abstract class CMTaskBase
    {
        // mcbtodo: make sure these properties get set before they are used
        public int CmSystemId { get; set; }

        public int CmFeatureId { get; set; }
        
        public int CmTaskId { get; set; }

        /// <summary>
        /// Called on a regular basis by the main program to have tasks auto-progress their state if appropriate.
        /// Note: A task that is changed to the reserved task state "Complete" is considered a complete task to the program.
        /// e.g.The dependency task that tracks other features may choose to check the state of another feature during this phase and if appropriate
        /// mark this task as Complete.
        /// Another manual type task may choose to do nothing here, letting the user completely control the task states.
        /// Also a task can use this function to set the default state of a task. When a task is first instanced by the program
        /// it will be in the "Instance" state.
        /// </summary>
        public abstract void AutoProgress();

        /// <summary>
        /// Returns the user control that should be rendered in the main UI that represents this task item.
        /// Note that the state of a task should not be included in this UI, as this is provided by the main program.
        /// </summary>
        /// <returns></returns>
        public abstract UserControl GetUI();
    }
}
