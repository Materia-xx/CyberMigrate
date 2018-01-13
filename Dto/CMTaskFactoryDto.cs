namespace Dto
{
    public class CMTaskFactoryDto : IdBasedObject
    {
        /// <summary>
        /// The name of the task factory
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The version that the task factory is currently at.
        /// When a task factory is first discovered this value is set in the database
        /// A task factory should check this value upon Init and upgrade any of its 
        /// table schemas in the database if the code requires.
        /// </summary>
        public int Version { get; set; }
    }
}
