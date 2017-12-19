using CyberMigrateCommom;
using Dto;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// Provides additional functions for interacting with the Systems table that are not provided in the base CRUD class
    /// </summary>
    public class CMSystemsCRUD : CMDataProviderCRUD<CMSystem>
    {
        public CMSystemsCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
        {
        }

        /// <summary>
        /// Returns the first system with the given name or null
        /// </summary>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public CMSystem Get_ForSystemName(string systemName)
        {
            return GetAll().FirstOrDefault(s => s.Name.Equals(systemName, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
