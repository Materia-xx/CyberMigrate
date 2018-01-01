﻿using DataProvider;
using DataProvider.Events;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBase.Extensions;

namespace Tasks.BuiltIn.Note
{
    public static class NoteExtensions
    {
        internal static void Note_CreateTaskDataInstance(CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance, int featureDepth)
        {
            // The task data (template) to clone
            var taskDataTemplate = BuildInTasksDataProviders.NoteDataProvider.Get_ForTaskId(cmTaskTemplate.Id);

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

            var opResult = BuildInTasksDataProviders.NoteDataProvider.Insert(taskData);
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
            var taskData = BuildInTasksDataProviders.NoteDataProvider.Get_ForTaskId(cmTask.Id);

            var newNote = FeatureVars.ResolveFeatureVarsInString(taskData.Note, featureVars);
            if (!newNote.Equals(taskData.Note, StringComparison.OrdinalIgnoreCase))
            {
                taskData.Note = newNote; // mcbtodo: if this line is removed it will cause a stack overflow, see if there is a way to keep track of what id is updating where in the CRUD providers to automatically catch this type of thing

                var opUpdateTaskData = BuildInTasksDataProviders.NoteDataProvider.Update(taskData);
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
