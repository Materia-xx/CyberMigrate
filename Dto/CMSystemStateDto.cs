namespace Dto
{
    public class CMSystemStateDto : IdBasedObject
    {
        public int CMSystemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
    }
}
