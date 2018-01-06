using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// Provides additional functions for interacting with the Systems table that are not provided in the base CRUD class
    /// </summary>
    public class CMSystemsCRUD : CMDataProviderCRUDBase<CMSystemDto>
    {
        public CMSystemsCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Returns all systems in the datastore
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<CMSystemDto> GetAll()
        {
            var results = base.GetAll();
            return results.OrderBy(s => s.Name);
        }

        /// <summary>
        /// Returns the first system with the given name or null
        /// </summary>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public CMSystemDto Get_ForSystemName(string systemName)
        {
            var results = Find(s =>
                s.Name.Equals(systemName, System.StringComparison.Ordinal) // Note: case 'sensitive' compare so we allow renames to upper/lower case
            );
            return results.FirstOrDefault();
        }

        private CMCUDResult UpsertChecks(CMCUDResult opResult, CMSystemDto dto)
        {
            // If we are checking an insert operation
            if (dto.Id == 0)
            {
                if (Get_ForSystemName(dto.Name) != null)
                {
                    opResult.Errors.Add($"A system with the name '{dto.Name}' already exists. Rename that one first.");
                }
            }
            // If we are checking an update operation
            {
                // Look for systems with this name that are not this record
                var dupeResults = Find(s =>
                    s.Id != dto.Id
                    && s.Name.Equals(dto.Name, System.StringComparison.Ordinal) // Note: case 'sensitive' compare so we allow renames to upper/lower case
                );

                if (dupeResults.Any())
                {
                    opResult.Errors.Add($"A system with the name '{dto.Name}' already exists.");
                    return opResult;
                }
            }

            return opResult;
        }

        public override CMCUDResult Insert(CMSystemDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMSystemDto updatingObject)
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
        public CMCUDResult UpdateIfNeeded_Name(int cmSystemId, string name)
        {
            var opResult = new CMCUDResult();
            var dbEntry = Get(cmSystemId);
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

            var refFeaturesCount = CMDataProvider.DataStore.Value.CMFeatures.Value.GetCount_InSystem(deletingId);

            if (refFeaturesCount > 0)
            {
                opResult.Errors.Add($"Cannot delete the item in {CollectionName} because there are features or feature templates that are still present.");
                return opResult;
            }

            return base.Delete(deletingId);
        }
    }
}
