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
                && s.Name.Equals(statemName, System.StringComparison.Ordinal) // Note: case 'sensitive' compare so we allow renames to upper/lower case
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
                if (validStates.Any(s => s.Id == rule.CMSystemStateId))
                {
                    // The state is already in our list, no need to add duplicates
                    continue;
                }

                // Get the state
                var state = CMDataProvider.DataStore.Value.CMSystemStates.Value.Get(rule.CMSystemStateId);
                if (state != null)
                {
                    validStates.Add(state);
                }
            }

            return validStates;
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMSystemStateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }

            return opResult;
        }



        public override CMCUDResult Insert(CMSystemStateDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            if (Get_ForStateName(insertingObject.Name, insertingObject.CMSystemId) != null)
            {
                opResult.Errors.Add($"Cannot insert an item into {CollectionName} because an item with the same name already exists within this system.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMSystemStateDto updatingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, updatingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            // Look for other items in the same system with the same name, that are not this item.
            var dupeNameResults = Find(s =>
                    s.CMSystemId == updatingObject.CMSystemId 
                    && s.Id != updatingObject.Id
                    && s.Name.Equals(updatingObject.Name, System.StringComparison.Ordinal) // Note: case 'sensitive' compare so we allow renames to upper/lower case
                );
            if (dupeNameResults.Any())
            { 
                opResult.Errors.Add($"Cannot update item in {CollectionName} because an item with the same name already exists within this system.");
                return opResult;
            }

            // Note: If a state's name is being updated this is okay to do without checking other refs. The refs should all refer to the state by the id
            // and will show the new name next time they are viewed.

            return base.Update(updatingObject);
        }

        public override CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();

            var originalState = Get(deletingId);

            // See if there are any features that referenced this state before deleting it
            var refStateTransitionRulesA = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ThatRef_SystemStateId(originalState.Id);
            var refStateTransitionRulesB = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ThatRef_ToCMSystemStateId(originalState.Id);

            if (refStateTransitionRulesA.Any() || refStateTransitionRulesB.Any())
            {
                opResult.Errors.Add($"Cannot delete item from {CollectionName} with id {deletingId} because there are feature state transition rules that still refer to it.");
                return opResult;
            }

            return base.Delete(deletingId);
        }
    }
}
