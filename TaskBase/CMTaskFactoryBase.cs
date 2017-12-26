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
        public abstract CMTaskBase GetTask(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId); // mcbtodo: why not just pass the 4 Dtos on these functions instead ?

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
            return new UserControl();
        }

        /// <summary>
        /// Called when instancing a task. A <see cref="CMTaskDto"/> will have already been created and is passed in.
        /// The task factory should create any task data for this new task and take care of updating the database.
        /// </summary>
        /// <param name="taskTypeName"></param>
        /// <param name="cmTaskInstanceId">The id of the newly created CMTaskDto instance that was created from the template</param>
        public abstract void CreateTaskDataInstance(string taskTypeName, int cmTaskInstanceId);

        /// <summary>
        /// Deletes task data for the specified task
        /// </summary>
        /// <param name="taskTypeName"></param>
        /// <param name="cmTaskId"></param>
        public abstract void DeleteTaskData(string taskTypeName, int cmTaskId);
    }
}
