using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMSystemStatesCRUD : CMDataProviderCRUDBase<CMSystemStateDto>
    {
        public CMSystemStatesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Gets a system state within the specified system by name
        /// </summary>
        /// <param name="statemName"></param>
        /// <returns></returns>
        public CMSystemStateDto Get_ForStateName(string statemName, int cmSystemId)
        {
            var results = Find(s =>
                s.CMSystemId == cmSystemId
                && s.Name.Equals(statemName, System.StringComparison.OrdinalIgnoreCase)
            );

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Get all system states that exist within the specified system.
        /// </summary>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public IEnumerable<CMSystemStateDto> GetAll_ForSystem(int cmSystemId)
        {
            var results = Find(s =>
                s.CMSystemId == cmSystemId
            );

            // Return with the lowest priority first. Same pattern as other places that use priority.
            return results.OrderBy(s => s.Priority);
        }

        /// <summary>
        /// Get all system states that are possible for the specified feature template
        /// </summary>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public IEnumerable<CMSystemStateDto> GetAll_ForFeatureTemplate(int cmFeatureId)
        {
            var validStates = new List<CMSystemStateDto>();

            var transitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(cmFeatureId);

            foreach (var rule in transitionRules)
            {
                if (validStates.Any(s => s.Id == rule.ToCMSystemStateId))
                {
                    // The state is already in our list, no need to add duplicates
                    continue;
                }

                // Get the state
                var state = CMDataProvider.DataStore.Value.CMSystemStates.Value.Get(rule.ToCMSystemStateId);
                if (state != null)
                {
                    validStates.Add(state);
                }
            }

            return validStates;
        }
    }
}
