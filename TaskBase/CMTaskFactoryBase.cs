using System.Collections.Generic;
using System.Windows.Controls;

namespace TaskBase
{
    public abstract class CMTaskFactoryBase
    {
        /// <summary>
        /// Get a list of task types that the task factory supports creating.
        /// The names should always be given in the format of 
        /// </summary>
        public List<string> SupportedTasks { get; set; } = new List<string>();

        /// <summary>
        /// Name of the task factory
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a list of task states that the specified task type will set at any point.
        /// This list should not include the following reserved states, which are automatically added for all states.
        ///     * Complete  - Represents a task that is complete.
        ///     * Template  - A task that is a template will have this state
        ///     * Instance - When a task is copied from a template to an instance the instance will be at this state initially
        /// </summary>
        /// <param name="taskType"></param>
        /// <returns></returns>
        public abstract List<string> GetRequiredTaskStates(string taskType);

        public abstract CMTaskBase CreateTask(int cmSystemId, int cmFeatureId, int cmTaskId);

        /// <summary>
        /// Gets a configuration UI that can be used to configure the tasks that are supplied by this task factory.
        /// It is optional for the task factory to implement this. A blank panel will be used by default.
        /// </summary>
        /// <returns></returns>
        public virtual UserControl GetConfigurationUI()
        {
            // mcbtodo: flesh out the way that task factories will store their configuration data. It is assumed to be of a different structure for each tsk factory
            // mcbtodo: If there is a way to store it in the db, I'd like to do that.. and just feed some sort of base class type back and forth to the config ui
            // mcbtodo: This will make it easier on the task factory development in that it doesn't need to read a file and go through a deserializiation process
            return new UserControl();
        }
    }
}
