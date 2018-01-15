namespace Dto
{
    /// <summary>
    /// Keeps track of user preferences in regards to window sizes and position.
    /// Not all settings use all properties.
    /// </summary>
    public class CMDimensionInfoDto : IdBasedObject
    {
        public string Name { get; set; }

        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }
    }
}
