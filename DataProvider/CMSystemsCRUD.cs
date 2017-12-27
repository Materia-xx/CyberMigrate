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

        public override CMCUDResult Insert(CMSystemDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (Get_ForSystemName(insertingObject.Name) != null)
            {
                opResult.Errors.Add($"A system with the name '{insertingObject.Name}' already exists. Rename that one first.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMSystemDto updatingObject)
        {
            var opResult = new CMCUDResult();

            // Look for systems with this name that are not this record
            var dupeResults = Find(s =>
                s.Id != updatingObject.Id
                && s.Name.Equals(updatingObject.Name, System.StringComparison.Ordinal) // Note: case 'sensitive' compare so we allow renames to upper/lower case
            );

            if (dupeResults.Any())
            {
                opResult.Errors.Add($"A system with the name '{updatingObject.Name}' already exists.");
                return opResult;
            }

            return base.Update(updatingObject);
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
