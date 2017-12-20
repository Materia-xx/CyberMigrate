namespace Dto
{
    public class CMDataStoreDto : IdBasedObject
    {
        /// <summary>
        /// The directory location on disk where the data resides
        /// </summary>
        public string StorePath { get; set; }
    }
}
