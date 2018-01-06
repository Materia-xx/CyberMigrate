using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    public class CMFeaturesCRUD : CMDataProviderCRUDBase<CMFeatureDto>
    {
        public CMFeaturesCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Finds the first feature by name under the given system.
        /// There should be only 1 if the program is working correct in keeping duplicate names out
        /// </summary>
        /// <param name="featureName"></param>
        /// <param name="cmSystemId"></param>
        /// <returns></returns>
        public CMFeatureDto Get_ForName(string featureName, int cmSystemId, bool isTemplate)
        {
            var results = Find(f => 
                (isTemplate ? f.CMParentFeatureTemplateId == 0 : f.CMParentFeatureTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
             && f.CMSystemId == cmSystemId
             && f.Name.Equals(featureName, System.StringComparison.Ordinal)); // Note: case 'sensitive' compare so we allow renames to upper/lower case

            return results.FirstOrDefault();
        }

        public IEnumerable<CMFeatureDto> GetAll_Templates()
        {
            var results = Find(f => f.CMParentFeatureTemplateId == 0);
            return results;
        }

        public IEnumerable<CMFeatureDto> GetAll_Instances()
        {
            var results = Find(f => f.CMParentFeatureTemplateId != 0);
            return results;
        }

        public Dictionary<int, CMFeatureDto> GetAll_Instances_AsLookup()
        {
            var lookup = new Dictionary<int, CMFeatureDto>();

            var results = Find(f => f.CMParentFeatureTemplateId != 0);
            foreach (var item in results)
            {
                lookup[item.Id] = item;
            }

            return lookup;
        }

        /// <summary>
        /// Gets all features within the specified system
        /// </summary>
        /// <param name="cmSystemId"></param>
        /// <param name="isTemplate"></param>
        /// <returns></returns>
        public IEnumerable<CMFeatureDto> GetAll_ForSystem(int cmSystemId, bool isTemplate)
        {
            var results = Find(f =>
                (isTemplate ? f.CMParentFeatureTemplateId == 0 : f.CMParentFeatureTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
             && f.CMSystemId == cmSystemId);

            return results.OrderBy(f => f.Name);
        }

        /// <summary>
        /// Gets all feature instances that are in the specified system state.
        /// </summary>
        /// <param name="cmSystemStateId"></param>
        /// <returns></returns>
        public IEnumerable<CMFeatureDto> GetAll_Instances_ForSystemState(int cmSystemStateId)
        {
            var results = Find(f =>
                f.CMParentFeatureTemplateId != 0
             && f.CMSystemStateId == cmSystemStateId);

            return results.OrderBy(f => f.Name);
        }

        public int GetCount_InSystem(int cmSystemId)
        {
            var results = Count(f =>
                f.CMSystemId == cmSystemId);

            return results;
        }

        /// <summary>
        /// Checks that apply to both insert and update operations
        /// </summary>
        /// <param name="opResult"></param>
        /// <returns></returns>
        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMFeatureDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                opResult.Errors.Add($"Name cannot be empty for an item in {CollectionName}");
            }

            if (dto.CMSystemId == 0)
            {
                opResult.Errors.Add($"An item in {CollectionName} must have a valid {nameof(CMFeatureDto.CMSystemId)}");
            }

            // Only instances are required to have a valid system state
            if (dto.CMParentFeatureTemplateId != 0)
            {
                if (dto.CMSystemStateId == 0)
                {
                    opResult.Errors.Add($"An item in {CollectionName} must have a valid {nameof(CMFeatureDto.CMSystemStateId)}");
                }
            }

            // If we are checking an insert operation
            if (dto.Id == 0)
            {
                // Require that template names are distinct. Instances can have duplicate names.
                if (dto.IsTemplate)
                {
                    if (Get_ForName(dto.Name, dto.CMSystemId, dto.IsTemplate) != null)
                    {
                        opResult.Errors.Add($"A feature with the name '{dto.Name}' already exists within the system. Rename that one first.");
                    }
                }
            }
            // If we are checking an update operation
            else
            {
                // Require that template names are distinct. Instances can have duplicate names.
                if (dto.IsTemplate)
                {
                    // Find a record with the same name that is not this one
                    var dupeResults = Find(f =>
                        f.Id != dto.Id
                        && (dto.IsTemplate ? f.CMParentFeatureTemplateId == 0 : f.CMParentFeatureTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
                        && f.CMSystemId == dto.CMSystemId
                        && f.Name.Equals(dto.Name, System.StringComparison.Ordinal)); // Note: case 'sensitive' compare so we allow renames to upper/lower case

                    if (dupeResults.Any())
                    {
                        opResult.Errors.Add($"A feature with the name '{dto.Name}' already exists within the system.");
                    }
                }
            }

            return opResult;
        }

        public override CMCUDResult Insert(CMFeatureDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMFeatureDto updatingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, updatingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Update(updatingObject);
        }

        /// <summary>
        /// Updates only the name if it has changed from the database.
        /// </summary>
        /// <param name="updatingObject"></param>
        /// <returns></returns>
        public CMCUDResult UpdateIfNeeded_Name(int cmFeatureId, string name)
        {
            var opResult = new CMCUDResult();
            var dbEntry = Get(cmFeatureId);
            if (dbEntry.Name.Equals(name, System.StringComparison.Ordinal))
            {
                // Nothing changed, no update to the name is needed
                return opResult;
            }

            // Update just the name
            dbEntry.Name = name;
            return Update(dbEntry);
        }

        public override CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();

            var deletingItem = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(deletingId);

            // Do not allow deleting of features if there are still tasks assigned to it.
            var featureTasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(deletingId, deletingItem.IsTemplate);
            if (featureTasks.Any())
            {
                opResult.Errors.Add($"Cannot delete item in {CollectionName} because tasks are still assigned.");
                return opResult;
            }

            // Also require any state transition rules to be removed
            var stateTransitionRules = CMDataProvider.DataStore.Value.CMFeatureStateTransitionRules.Value.GetAll_ForFeatureTemplate(deletingId);
            if (stateTransitionRules.Any())
            {
                opResult.Errors.Add($"Cannot delete item in {CollectionName} because there are feature state transition rules still associated with it.");
                return opResult;
            }

            return base.Delete(deletingId);
        }

    }
}
