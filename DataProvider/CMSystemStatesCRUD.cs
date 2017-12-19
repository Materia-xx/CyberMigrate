using CyberMigrateCommom;
using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMSystemStatesCRUD : CMDataProviderCRUD<CMSystemState>
    {
        public CMSystemStatesCRUD(string dataStoreDbPath, string collectionName) : base(dataStoreDbPath, collectionName)
        {
        }

        /// <summary>
        /// Indicates if a system state with the given name already exists
        /// </summary>
        /// <param name="statemName"></param>
        /// <returns></returns>
        public CMSystemState Get_ForStateName(string statemName)
        {
            // mcbtodo: I don't like how this routes through getAll first, pass a lambda down instead
            return GetAll().FirstOrDefault(s => s.Name.Equals(statemName, System.StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<CMSystemState> GetAll_ForSystem(int cmSystemId)
        {
            return GetAll().Where(s => s.CMSystemId == cmSystemId);
        }
    }
}
