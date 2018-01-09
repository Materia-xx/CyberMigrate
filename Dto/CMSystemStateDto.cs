namespace Dto
{
    public class CMSystemStateDto : IdBasedObject
    {
        public int CMSystemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Priority is the order in which associated tasks should be listed in the UI.
        /// This may be different than MigrationOrder if certain aspects of the migration
        /// are more important.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Migration order is the order in which one system state progresses to the next.
        /// e.g. Plan, Dev, Code Review, Check in, Complete.
        /// </summary>
        public int MigrationOrder { get; set; }
    }
}
