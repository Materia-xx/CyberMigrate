using DataProvider;
using DataProvider.Events;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskBase.Extensions;

namespace Tasks.BuiltIn.Note
{
    public static class NoteExtensions
    {
        internal static NoteTaskDataCRUD NoteDataProvider
        {
            get
            {
                if (noteDataProvider == null)
                {
                    noteDataProvider = new NoteTaskDataCRUD(nameof(NoteDto));
                }
                return noteDataProvider;
            }
        }
        private static NoteTaskDataCRUD noteDataProvider;

        internal static void Note_CreateTaskDataInstance(CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance)
        {
            // The task data (template) to clone
            var taskDataTemplate = NoteDataProvider.Get_ForTaskId(cmTaskTemplate.Id);

            // If there was no task data template defined then just return without creating data for the instance
            if (taskDataTemplate == null)
            {
                return;
            }

            // Now we can create new task data note
            var taskData = new NoteDto()
            {
                TaskId = cmTaskInstance.Id,
                Note = taskDataTemplate.Note
            };

            var opResult = NoteDataProvider.Insert(taskData);
            if (opResult.Errors.Any())
            {
                throw new Exception(opResult.ErrorsCombined);
            }
        }

        internal static void Note_ResolveFeatureVars(CMTaskDto cmTask, List<CMFeatureVarStringDto> featureVars)
        {
            // Do not resolve feature vars for a task template
            if (cmTask.IsTemplate)
            {
                return;
            }

            // The task data for the task
            var taskData = NoteDataProvider.Get_ForTaskId(cmTask.Id);

            var newNote = FeatureVars.ResolveFeatureVarsInString(taskData.Note, featureVars);
            if (!newNote.Equals(taskData.Note, StringComparison.OrdinalIgnoreCase))
            {
                taskData.Note = newNote;

                var opUpdateTaskData = NoteDataProvider.Update(taskData);
                if (opUpdateTaskData.Errors.Any())
                {
                    throw new InvalidOperationException(opUpdateTaskData.ErrorsCombined);
                }
            }
        }

        internal static void NoteData_Created_ResolveFeatureVars(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs)
        {
            // The note data that was created
            var noteData = createdRecordEventArgs.CreatedDto as NoteDto;

            // The task that this note data is associated with
            var task = CMDataProvider.DataStore.Value.CMTasks.Value.Get(noteData.TaskId);

            // Get all feature vars that currently exist for the feature that the task is in
            var featureVars = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(task.CMFeatureId).ToList();

            // Resolve any templates in the newly created note
            Note_ResolveFeatureVars(task, featureVars);
        }

        internal static void NoteData_Updated_ResolveFeatureVars(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs)
        {
            // The note data that was created
            var noteData = updatedRecordEventArgs.DtoAfter as NoteDto;

            // The task that this note data is associated with
            var task = CMDataProvider.DataStore.Value.CMTasks.Value.Get(noteData.TaskId);

            // Get all feature vars that currently exist for the feature that the task is in
            var featureVars = CMDataProvider.DataStore.Value.CMFeatureVarStrings.Value.GetAll_ForFeature(task.CMFeatureId).ToList();

            // Resolve any templates in the updated note
            Note_ResolveFeatureVars(task, featureVars);
        }
    }
}
