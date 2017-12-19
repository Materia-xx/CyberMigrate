using CyberMigrateCommom;
using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeatureTemplatesCRUD : CMDataProviderCRUD<CMFeatureTemplate>
    {
        public CMFeatureTemplatesCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
        {
        }

        /// <summary>
        /// Finds a feature template by name under the given system
        /// </summary>
        /// <param name="featureTemplateName"></param>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public CMFeatureTemplate Get_ForFeatureTemplateName(string featureTemplateName, int cmSystemId)
        {
            // mcbtodo: I don't like how this routes through getAll first, pass a lambda down instead
            // mcbtodo: track down everywhere that uses GetAll and rewrite them... it may be nothing, it may help
            return GetAll().FirstOrDefault(s => s.Name.Equals(featureTemplateName, System.StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<CMFeatureTemplate> GetAll_ForSystem(int cmSystemId)
        {
            return GetAll().Where(s => s.CMSystemId == cmSystemId);
        }
    }
}
