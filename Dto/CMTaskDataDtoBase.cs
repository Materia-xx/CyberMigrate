namespace Dto
{
    /// <summary>
    /// Task plugins should inherit from this when they need a Dto to store in the database
    /// </summary>
    public class CMTaskDataDtoBase : IdBasedObject
    {
        /// <summary>
        /// The task id that is linked to the data
        /// </summary>
        public int TaskId { get; set; }
    }
}
