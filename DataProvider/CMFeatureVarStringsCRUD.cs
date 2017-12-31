using Dto;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// Provides a templated CRUD interface for string based feature vars
    /// </summary>
    public class CMFeatureVarStringsCRUD : CMDataProviderCRUDBase<CMFeatureVarStringDto>
    {
        public CMFeatureVarStringsCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Gets all feature vars that are attached to the feature
        /// </summary>
        /// <param name="cmFeatureId"></param>
        /// <returns></returns>
        public IEnumerable<CMFeatureVarStringDto> GetAll_ForFeature(int cmFeatureId)
        {
            var results = Find(v => v.CMFeatureId == cmFeatureId);
            return results;
        }

        public override CMCUDResult Delete(int deletingId)
        {
            throw new Exception("Feature vars are immutable and cannot be deleted after they are created.");
        }

        public override CMCUDResult Update(CMFeatureVarStringDto updatingObject)
        {
            throw new Exception("Feature vars are immutable and cannot be updated after they are created.");
        }

        public override CMCUDResult Insert(CMFeatureVarStringDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (insertingObject.CMFeatureId == 0)
            {
                opResult.Errors.Add($"An item in {CollectionName} must be assigned to a feature.");
                return opResult;
            }

            if (string.IsNullOrWhiteSpace(insertingObject.Name))
            {
                opResult.Errors.Add($"An item in {CollectionName} must have a name.");
                return opResult;
            }

            // Check for another feature var with the same name in this same feature
            var existingFeatureVar = Find(v =>
                v.CMFeatureId == insertingObject.CMFeatureId
                && v.Name.Equals(insertingObject.Name, StringComparison.OrdinalIgnoreCase) // Names are immutable and replacements of feature vars is also done in a case-insensitive manner
            );
            if (existingFeatureVar.Any())
            {
                opResult.Errors.Add($"There is already an item in {CollectionName} with the name {insertingObject.Name}. Duplicate entries are not allowed.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }
    }
}
