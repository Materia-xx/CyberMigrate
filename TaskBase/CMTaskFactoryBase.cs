using System.Collections.Generic;
using System.Windows.Controls;

namespace TaskBase
{
    public abstract class CMTaskFactoryBase
    {
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

        /// <summary>
        /// Get a list of task types that the task factory supports creating.
        /// The suggested format is to use nameof() with the task classes that this factory will produce.
        /// </summary>
        public abstract List<string> GetTaskTypes();

        /// <summary>
        /// Called when the program needs an instance of the task in order to call task specific methods.
        /// </summary>
        /// <param name="taskTypeName"></param>
        /// <param name="cmSystemId"></param>
        /// <param name="cmFeatureId"></param>
        /// <param name="cmTaskId"></param>
        /// <returns></returns>
        public abstract CMTaskBase GetTask(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId);

        /// <summary>
        /// The UI that is shown when editing a task
        /// </summary>
        /// <param name="taskTypeName"></param>
        /// <param name="cmSystemId"></param>
        /// <param name="cmFeatureId"></param>
        /// <param name="cmTaskId"></param>
        /// <returns></returns>
        public abstract UserControl GetTaskUI(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId);

        /// <summary>
        /// Gets a configuration UI that can be used to configure each task type that are supplied by this task factory.
        /// It is optional for the task factory to implement this. A blank panel will be used by default.
        /// </summary>
        /// <returns></returns>
        public virtual UserControl GetTaskConfigUI(string taskTypeName)
        {
            // mcbtodo: flesh out the way that task factories will store their configuration data. It is assumed to be of a different structure for each task factory
            // mcbtodo: If there is a way to store it in the db, I'd like to do that.. and just feed some sort of base class type back and forth to the config ui
            // mcbtodo: This will make it easier on the task factory development in that it doesn't need to read a file and go through a deserializiation process
            return new UserControl();
        }
    }
}
