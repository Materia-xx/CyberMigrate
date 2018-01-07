using DataProvider;
using DataProvider.Events;
using Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using TaskBase;
using TaskBase.Extensions;
using Tasks.BuiltIn.FeatureDependency;
using Tasks.BuiltIn.Note;

namespace Tasks.BuiltIn
{
    [Export(typeof(CMTaskFactoryBase))]
    public class CMBuiltInTaskFactory : CMTaskFactoryBase
    {
        public override string Name
        {
            get
            {
                return nameof(CMBuiltInTaskFactory);
            }
        }

        public override List<string> GetTaskTypes()
        {
            var taskTypes = new List<string>();
            taskTypes.Add(nameof(BuildInTaskTypes.FeatureDependency));
            taskTypes.Add(nameof(BuildInTaskTypes.Note));
            return taskTypes;
        }

        public override List<string> GetRequiredTaskStates(CMTaskTypeDto cmTaskType)
        {
            var requiredStates = new List<string>();

            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    requiredStates.Add(nameof(FeatureDependencyTaskStateNames.WaitingOnDependency));
                    break;
                case nameof(BuildInTaskTypes.Note):
                    // No required states in the note task
                    break;
            }

            return requiredStates;
        }

        public override UserControl GetTaskConfigUI(CMTaskTypeDto cmTaskType)
        {
            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    var configUI = new FeatureDependencyConfigUC();
                    return configUI;
                case nameof(BuildInTaskTypes.Note):
                    // No config UI for the note task
                    return null;
            }

            return null;
        }

        public override UserControl GetTaskUI(CMTaskTypeDto cmTaskType, CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    var featureDependencyTaskUI = new FeatureDependencyUC(cmSystem, cmFeature, cmTask);
                    return featureDependencyTaskUI;
                case nameof(BuildInTaskTypes.Note):
                    var noteTaskUI = new NoteUC(cmSystem, cmFeature, cmTask);
                    return noteTaskUI;
            }

            return null;
        }

        public override void CreateTaskDataInstance(CMTaskTypeDto cmTaskType, CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance)
        {
            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    FeatureDependencyExtensions.FeatureDependency_CreateTaskDataInstance(cmTaskTemplate, cmTaskInstance);
                    break;
                case nameof(BuildInTaskTypes.Note):
                    NoteExtensions.Note_CreateTaskDataInstance(cmTaskTemplate, cmTaskInstance);
                    break;
            }
        }

        public override void Initialize()
        {
            var featureDependencyTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(nameof(BuildInTaskTypes.FeatureDependency));
            FeatureDependencyExtensions.FeatureDependency_TaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(featureDependencyTaskType.Id).ToList();
            FeatureDependencyExtensions.FeatureDependency_TaskState_WaitingOnDependency = FeatureDependencyExtensions.FeatureDependency_TaskStates.First(s => s.InternalName.Equals(nameof(FeatureDependencyTaskStateNames.WaitingOnDependency)));
            FeatureDependencyExtensions.FeatureDependency_TaskState_Closed = FeatureDependencyExtensions.FeatureDependency_TaskStates.First(s => s.InternalName.Equals(ReservedTaskStates.Closed));

            // mcbtodo: figure out why typing += <tab><tab> here doesn't do anything, but yet the events are working fine.
            CMDataProvider.DataStore.Value.CMTasks.Value.OnBeforeRecordDeleted += Task_Deleted;

            // If a feature is somehow deleted then any feature dependency that was pointing at it can be resolved
            // If a feature state is changed to the one being monitored for then it can be resolved
            // If a feature is inserted and a dependency was already watching that feature id ... no, that doesn't make sense.
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnBeforeRecordDeleted += Feature_Deleted;
            CMDataProvider.DataStore.Value.CMFeatures.Value.OnRecordUpdated += Feature_Updated;

            // Any time a feature var is updated we want to make sure the appropriate task data is taken care of
            CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.OnRecordCreated += FeatureVar_Created;

            // Any time a note data is created or updated we want to re-apply any feature vars in it
            NoteExtensions.NoteDataProvider.OnRecordCreated += NoteExtensions.NoteData_Created_ResolveFeatureVars;
            NoteExtensions.NoteDataProvider.OnRecordUpdated += NoteExtensions.NoteData_Updated_ResolveFeatureVars;

            // Handle anything we need to anytime a feature dependency task data is created or updated
            // Feature dependency data deleted:
            //      * If the associated task was deleted too then let that event handler handle it.
            //      * If just the data was deleted then don't delete the child feature if there is one.
            FeatureDependencyExtensions.FeatureDependencyDataProvider.OnRecordCreated += FeatureDependencyExtensions.FeatureDependencyData_Created;
            FeatureDependencyExtensions.FeatureDependencyDataProvider.OnRecordUpdated += FeatureDependencyExtensions.FeatureDependencyData_Updated;
        }

        private void Task_Deleted(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs)
        {
            var cmTaskDto = deletedRecordEventArgs.DtoBefore as CMTaskDto;
            // Try to figure out what the task type is
            var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForTaskId(cmTaskDto.Id);
            if (cmTaskType == null)
            {
                return;
            }

            switch (cmTaskType.Name)
            {
                case nameof(BuildInTaskTypes.FeatureDependency):
                    FeatureDependencyExtensions.FeatureDependencyDataProvider.Delete_ForTaskId(cmTaskDto.Id);
                    break;
                case nameof(BuildInTaskTypes.Note):
                    NoteExtensions.NoteDataProvider.Delete_ForTaskId(cmTaskDto.Id);
                    break;
            }
        }

        private void Feature_Deleted(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs)
        {
            var beforeDto = deletedRecordEventArgs.DtoBefore as CMFeatureDto;

            FeatureDependencyExtensions.UpdateTaskStatesForFeatureDependendies(beforeDto, null);
        }

        private void Feature_Updated(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            // Figure out if the feature state was updated, which is what we're really interested in here.
            var beforeDto = updatedRecordEventArgs.DtoBefore as CMFeatureDto;
            var afterDto = updatedRecordEventArgs.DtoAfter as CMFeatureDto;

            // Currently updates to feature templates system status doesn't happen, but if it starts at some point, just go with it until it becomes and issue

            // If the feature system state was updated
            if (beforeDto.CMSystemStateId != afterDto.CMSystemStateId)
            {
                FeatureDependencyExtensions.UpdateTaskStatesForFeatureDependendies(beforeDto, afterDto);
            }
        }

        private void FeatureVar_Created(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs)
        {
            // The featureVar that was added
            var featureVar = createdRecordEventArgs.CreatedDto as CMFeatureVarStringDto;

            // The feature that the feature var is assigned to
            var feature = CMDataProvider.DataStore.Value.CMFeatures.Value.Get(featureVar.CMFeatureId);

            // Don't process feature var replacements in a feature template
            if (feature.IsTemplate)
            {
                return;
            }

            // All current feature vars for this feature
            var featureVars = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(feature.Id).ToList();

            // All tasks currently assigned to the feature
            var tasks = CMDataProvider.DataStore.Value.CMTasks.Value.GetAll_ForFeature(feature.Id);

            foreach (var cmTask in tasks)
            {
                // Figure out if this is a task type we are interested in
                var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get(cmTask.CMTaskTypeId);

                switch (cmTaskType.Name)
                {
                    case nameof(BuildInTaskTypes.FeatureDependency):
                        FeatureDependencyExtensions.FeatureDependency_ResolveFeatureVars(cmTask, featureVars);
                        break;
                    case nameof(BuildInTaskTypes.Note):
                        NoteExtensions.Note_ResolveFeatureVars(cmTask, featureVars);
                        break;
                }
            }
        }
    }
}
