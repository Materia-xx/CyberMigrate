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

        public IEnumerable<CMFeatureDto> GetAll(bool isTemplate)
        {
            var results = Find(f =>
                (isTemplate ? f.CMParentFeatureTemplateId == 0 : f.CMParentFeatureTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
            );

            return results;
        }

        public IEnumerable<CMFeatureDto> GetAll_ForSystem(int cmSystemId, bool isTemplate)
        {
            var results = Find(f =>
                (isTemplate ? f.CMParentFeatureTemplateId == 0 : f.CMParentFeatureTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
             && f.CMSystemId == cmSystemId);

            return results.OrderBy(f => f.Name);
        }

        public int GetCount_InSystem(int cmSystemId)
        {
            var results = Count(f =>
                f.CMSystemId == cmSystemId);

            return results;
        }

        public override CMCUDResult Insert(CMFeatureDto insertingObject)
        {
            // Require that template names are distinct. Instances can have duplicate names.
            if (insertingObject.IsTemplate)
            {
                var opResult = new CMCUDResult();

                if (Get_ForName(insertingObject.Name, insertingObject.CMSystemId, insertingObject.IsTemplate) != null)
                {
                    opResult.Errors.Add($"A feature with the name '{insertingObject.Name}' already exists within the system. Rename that one first.");
                    return opResult;
                }
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMFeatureDto updatingObject)
        {
            // Require that template names are distinct. Instances can have duplicate names.
            if (updatingObject.IsTemplate)
            {
                var opResult = new CMCUDResult();

                // Find a record with the same name that is not this one
                var dupeResults = Find(f =>
                    f.Id != updatingObject.Id
                    && (updatingObject.IsTemplate ? f.CMParentFeatureTemplateId == 0 : f.CMParentFeatureTemplateId != 0) // Don't use IsTemplate Dto property here b/c this queries BSON data directly
                    && f.CMSystemId == updatingObject.CMSystemId
                    && f.Name.Equals(updatingObject.Name, System.StringComparison.Ordinal)); // Note: case 'sensitive' compare so we allow renames to upper/lower case

                if (dupeResults.Any())
                {
                    opResult.Errors.Add($"A feature with the name '{updatingObject.Name}' already exists within the system.");
                    return opResult;
                }
            }

            return base.Update(updatingObject);
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
