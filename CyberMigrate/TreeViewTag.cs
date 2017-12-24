using Dto;

namespace CyberMigrate
{
    /// <summary>
    /// Attached to nodes in tree views. Used to store information such as the Dto that each node represents
    /// </summary>
    public class TreeViewTag
    {
        public TreeViewTag(IdBasedObject dto)
        {
            this.Dto = dto;
        }

        public IdBasedObject Dto { get; private set; }

        public string DtoTypeName
        {
            get
            {
                if (Dto == null)
                {
                    return string.Empty;
                }
                return Dto.GetType().Name;
            }
        }
    }
}
