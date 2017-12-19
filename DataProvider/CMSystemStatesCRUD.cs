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
        /// Gets a system state within the specified system by name
        /// </summary>
        /// <param name="statemName"></param>
        /// <returns></returns>
        public CMSystemState Get_ForStateName(string statemName, int cmSystemId)
        {
            // First get all in the system
            var results = GetAll_ForSystem(cmSystemId);

            // Then filter it down to just the one with the name or default
            results = results.Where(s => s.Name.Equals(statemName, System.StringComparison.OrdinalIgnoreCase));
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Get all system states that exist within the specified system.
        /// </summary>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public IEnumerable<CMSystemState> GetAll_ForSystem(int cmSystemId)
        {
            Query query = Query.EQ(nameof(CMSystemState.CMSystemId), cmSystemId);
            var results = QueryCollection(query);

            // Return with the lowest priority first. Same pattern as other places that use priority.
            return results.OrderBy(s => s.Priority);
        }
    }
}
