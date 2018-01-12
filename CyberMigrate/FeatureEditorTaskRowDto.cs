using DataProvider;
using Dto;

namespace CyberMigrate
{
    /// <summary>
    /// Adds extra functionality to the CMTaskDto purely for the purpose of binding within the feature editor tasks section
    /// These are not on the base object because I don't want them serialized into the db.
    /// They are not extension methods because it seems like binding to that is more complex.
    /// </summary>
    internal class FeatureEditorTaskRowDto
    {
        private int closedStateId = 0;

        public CMTaskDto Task { get; private set; }

        internal FeatureEditorTaskRowDto()
        {
            this.Task = new CMTaskDto();
        }

        internal FeatureEditorTaskRowDto(CMTaskDto cmTask)
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
                // Figure out what the id of the closed state is if we don't know it iyet
                if (closedStateId == 0)
                {
                    closedStateId = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(ReservedTaskStates.Closed, this.Task.CMTaskTypeId).Id;
                }

                return this.Task.CMTaskStateId == closedStateId;
            }
        }

    }
}
