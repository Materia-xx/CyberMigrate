using Dto;
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
        /// The version that this task library code is currently at. It is expected that any data store database that uses this task library
        /// be at the same version. If it is not, then it will need to go through an upgrade phase.
        /// </summary>
        public abstract int Version { get; }

        /// <summary>
        /// Gets a list of task states that the specified task type will set at any point.
        /// This list should not include the following reserved states, which are automatically added for all states.
        ///     * Closed  - Represents a task that is closed.
        ///     * Template  - A task that is a template will have this state
        ///     * Instance - When a task is copied from a template to an instance the instance will be at this state initially
        /// </summary>
        /// <param name="taskType"></param>
        /// <returns></returns>
        public abstract List<string> GetRequiredTaskStates(CMTaskTypeDto cmTaskType);

        /// <summary>
        /// Get a list of task types that the task factory supports creating.
        /// The suggested format is to use nameof() with the task classes that this factory will produce.
        /// </summary>
        public abstract List<string> GetTaskTypes();

        /// <summary>
        /// The UI that is shown when editing a task
        /// </summary>
        /// <param name="cmTaskType"></param>
        /// <param name="cmSystem"></param>
        /// <param name="cmFeature"></param>
        /// <param name="cmTask"></param>
        /// <returns></returns>
        public abstract UserControl GetTaskUI(CMTaskTypeDto cmTaskType, CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask);

        /// <summary>
        /// Gets a configuration UI that can be used to configure each task type that are supplied by this task factory.
        /// It is optional for the task factory to implement this. A blank panel will be used by default.
        /// </summary>
        /// <returns></returns>
        public virtual UserControl GetTaskConfigUI(CMTaskTypeDto cmTaskType)
        {
            return new UserControl();
        }

        /// <summary>
        /// Called when instancing a task. A <see cref="CMTaskDto"/> will have already been created and is passed in.
        /// The task factory should create any task data for this new task and take care of updating the database.
        /// </summary>
        /// <param name="cmTaskType"></param>
        /// <param name="cmTaskTemplate">The task template that the task was created from</param>
        /// <param name="cmTaskInstance">The new task that was created</param>
        public abstract void CreateTaskDataInstance(CMTaskTypeDto cmTaskType, CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance);

        /// <summary>
        /// Called after registering the task types and states in the database.
        /// Here the task can subscribe to data provider events and other things needed.
        /// The Initialize function is called once at program startup for each task factory.
        /// </summary>
        public abstract void Initialize();
    }
}
