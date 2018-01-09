namespace Dto
{
    public class CMDataStoreInfoDto : IdBasedObject
    {
        /// <summary>
        /// The version that the database schema currently is.
        /// </summary>
        public int DatabaseSchemaVersion { get; set; }
    }
}
