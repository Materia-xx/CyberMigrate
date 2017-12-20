using System.Windows.Controls;

namespace TaskBase
{
    /// <summary>
    /// All tasks should inherit from this base class in order to be recognized as tasks that can be used by the program
    /// </summary>
    public abstract class TaskBase
    {
        protected int CmSystemId;
        protected int CmFeatureId;
        protected int CmTaskId;

        /// <summary>
        /// The main program will take care of setting up a directory to hold task data for the task if it has not already been done.
        /// The task base holds these values (systemId, featureId, taskId) in memory for when it needs to call back to the shared library.
        /// The task does not need to cache these values to the taskdata.json however .
        /// </summary>
        /// <param name="cmSystemId"></param>
        /// <param name="cmFeatureId"></param>
        /// <param name="cmTaskId"></param>
        public TaskBase(int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            this.CmSystemId = cmSystemId;
            this.CmFeatureId = cmFeatureId;
            this.CmTaskId = cmTaskId;
        }

        /// <summary>
        /// Called on a regular basis by the main program to have tasks auto-progress their state if appropriate.
        /// Note: A task that is changed to the reserved task state "complete" is considered a complete task to the program.
        /// e.g.The dependency task that tracks other features may choose to check the state of another feature during this phase and if appropriate
        /// mark this task as complete.Another manual type task may choose to do nothing here, letting the user completely control the task states.
        /// </summary>
        public abstract void AutoProgress();

        /// <summary>
        /// Returns the user control that should be rendered in the main UI that represents this task item.
        /// Note that the state of a task should not be included in this UI, as this is provided by the main program.
        /// </summary>
        /// <returns></returns>
        public abstract UserControl GetUI();


        // mcbtodo: create another function to get the UI that is meant to configure the task. This UI will show up in the config screen.
    }
}
