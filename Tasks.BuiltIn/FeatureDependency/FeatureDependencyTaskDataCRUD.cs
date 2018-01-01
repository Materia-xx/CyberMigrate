using DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tasks.BuiltIn.FeatureDependency
{
    public class FeatureDependencyTaskDataCRUD : CMTaskDataCRUD<FeatureDependencyDto>
    {
        public FeatureDependencyTaskDataCRUD(string collectionName) : base(collectionName)
        {
        }

        public IEnumerable<FeatureDependencyDto> GetAll_ForInstancedFeatureId(int cmFeatureId)
        {
            var results = Find(t => t.InstancedCMFeatureId == cmFeatureId);
            return results;
        }

        private CMCUDResult UpsertChecks(CMCUDResult opResult, FeatureDependencyDto dto)
        {
            foreach (var taskDataRow in dto.PathOptions)
            {
                if (taskDataRow.CMFeatureTemplateId == 0)
                {
                    opResult.Errors.Add("Each row option in a feature dependency must be assigned to a valid feature.");
                }

                if (taskDataRow.CMTargetSystemStateId == 0)
                {
                    opResult.Errors.Add("Each row option in a feature dependency must be assigned to a valid sytem state.");
                }

                if (string.IsNullOrWhiteSpace(taskDataRow.FeatureVarName) && !string.IsNullOrWhiteSpace(taskDataRow.FeatureVarSetTo))
                {
                    opResult.Errors.Add("Cannot set a feature var value to check for without specifying the feature var itself (within a feature dependency).");
                }
            }

            // Make sure the options are listed in order by re-assigning them in order.
            dto.PathOptions = dto.PathOptions.OrderBy(po => po.Order).ToList();
            return opResult;
        }

        public override CMCUDResult Update(FeatureDependencyDto updatingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, updatingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Update(updatingObject);
        }

        public override CMCUDResult Insert(FeatureDependencyDto insertingObject)
        {
            var opResult = new CMCUDResult();
            opResult = UpsertChecks(opResult, insertingObject);
            if (opResult.Errors.Any())
            {
                return opResult;
            }

            return base.Insert(insertingObject);
        }
    }
}
