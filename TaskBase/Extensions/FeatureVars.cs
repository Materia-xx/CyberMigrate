using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskBase.Extensions
{
    public static class FeatureVars
    {
        /// <summary>
        /// Replaces feature vars found in a string.
        /// These come in the format of ${varname}
        /// e.g. A variable named ParentFeature.Name would be ${ParentFeature.Name}
        /// </summary>
        /// <param name="stringToResolve"></param>
        /// <param name="featureVars"></param>
        /// <returns></returns>
        public static string ResolveFeatureVarsInString(string stringToResolve, List<CMFeatureVarStringDto> featureVars)
        {
            if (stringToResolve == null)
            {
                return null;
            }

            foreach (var featureVar in featureVars)
            {
                var seeking = $"${{{featureVar.Name}}}";
                stringToResolve = stringToResolve.Replace(seeking, featureVar.Value);
            }

            return stringToResolve;
        }

        /// <summary>
        /// Resolves all of the feature vars currently possible for a feature
        /// </summary>
        /// <param name="feature"></param>
        public static void ResolveFeatureVarsForFeatureAndTasks(this CMFeatureDto feature)
        {
            // Nothing should be resolving feature vars for a feature template
            if (feature.IsTemplate)
            {
                throw new InvalidOperationException("Resolving feature vars for a feature template is not supported.");
            }

            var featureVars = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(feature.Id).ToList();

            // Currently the only thing that can use feature vars in a feature is the name.
            string newFeatureName = ResolveFeatureVarsInString(feature.Name, featureVars);

            // Only update the feature if an update was made
            if (!newFeatureName.Equals(feature.Name, StringComparison.OrdinalIgnoreCase))
            {
                feature.Name = newFeatureName;

                var opFeatureUpdate = CMDataProvider.DataStore.Value.CMFeatures.Value.Update(feature);
                if (opFeatureUpdate.Errors.Any())
                {
                    throw new Exception(opFeatureUpdate.ErrorsCombined);
                }
            }

            // Update the title on all tasks (task data updates are handled fully by the task factory catalog CUD callbacks)
            var tasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(feature.Id);
            foreach (var cmTask in tasks)
            {
                cmTask.ResolveFeatureVars(featureVars);
            }
        }

        public static void ResolveFeatureVars(this CMTaskDto task, List<CMFeatureVarStringDto> featureVars)
        {
            if (task.IsTemplate)
            {
                throw new InvalidOperationException("Resolving feature vars for a task template is not supported.");
            }

            // The only thing to resolve in a task itself is the title
            var newTaskTitle = ResolveFeatureVarsInString(task.Title, featureVars);

            // Only update the task if a change was made
            if (!newTaskTitle.Equals(task.Title, StringComparison.OrdinalIgnoreCase))
            {
                task.Title = newTaskTitle;

                var opTaskUpdate = CMDataProvider.DataStore.Value.CMTasks.Value.Update(task);
                if (opTaskUpdate.Errors.Any())
                {
                    throw new Exception(opTaskUpdate.ErrorsCombined);
                }
            }
        }
    }
}
