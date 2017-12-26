namespace CyberMigrate
{
    /// <summary>
    /// Represents a single row shown in the filter results grid shown in the main window
    /// </summary>
    public class FilterResultItem
    {
        /// <summary>
        /// The name of the system that the task exists it
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// The name of the system state that the task exists in
        /// </summary>
        public string SystemStateName { get; set; }

        /// <summary>
        /// The name of the feature that the task exists in
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// The task title
        /// </summary>
        public string TaskTitle { get; set; }

        /// <summary>
        /// The task Id
        /// </summary>
        public int TaskId { get; set; }
    }
}
