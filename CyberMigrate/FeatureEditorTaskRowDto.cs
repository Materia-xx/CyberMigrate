using DataProvider;
using Dto;
using TaskBase;

namespace CyberMigrate
{
    /// <summary>
    /// Adds extra functionality to the CMTaskDto purely for the purpose of binding within the feature editor tasks section
    /// These are not on the base object because I don't want them serialized into the db.
    /// They are not extension methods because it seems like binding to that is more complex.
    /// </summary>
    public class FeatureEditorTaskRowDto
    {
        private int closedStateId = 0;

        public CMTaskDto Task { get; private set; }

        public FeatureEditorTaskRowDto()
        {
            this.Task = new CMTaskDto();
        }

        public FeatureEditorTaskRowDto(CMTaskDto cmTask)
        {
            this.Task = cmTask;
        }

        /// <summary>
        /// Indicates if the task is in the closed state or not.
        /// </summary>
        public bool IsClosed
        {
            get
            {
                // Unknown task or task type, consider it un-closed
                if (this.Task == null || this.Task.CMTaskTypeId == 0)
                {
                    return false;
                }

                // Figure out what the id of the closed state is if we don't know it yet
                // mcbtodo: this hints that once the task type is set that it cannot be changed, validate this for real in the CRUD update
                if (closedStateId == 0)
                {
                    closedStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Closed, this.Task.CMTaskTypeId).Id;
                }

                return this.Task.CMTaskStateId == closedStateId;
            }
        }

        public string EditTaskDataButtonText
        {
            get
            {
                bool hasTaskData = false;
                if (this.Task == null || this.Task.CMTaskTypeId == 0)
                {
                    // Unknown task or task type, consider it to have no task data yet
                }
                else
                {
                    var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(Task.CMTaskTypeId);
                    hasTaskData = TaskFactoriesCatalog.Instance.HasTaskData(cmTaskType, Task);
                }

                if (hasTaskData)
                {
                    return "View";
                }
                else
                {
                    return "Create Task Data";
                }
            }
        }

    }
}
