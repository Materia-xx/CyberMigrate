﻿using DataProvider;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBase;

namespace CyberMigrate
{
    /// <summary>
    /// Task factory init routines
    /// </summary>
    internal static class TaskFactories
    {
        /// <summary>
        /// Keeps track if each tack factory has been called to be initialized so we
        /// don't end up calling the same factory 2 times.
        /// </summary>
        private static Dictionary<string, bool> taskFactoryInitialized = new Dictionary<string, bool>();

        /// <summary>
        /// Taskes care of scanning for task factories and registering them in the database if needed.
        /// </summary>
        /// <returns></returns>
        private static string RegisterTaskFactories_InDatabase()
        {
            var taskFactories = TaskFactoriesCatalog.Instance.TaskFactories.ToList();

            var currentFactoriesFromDisk = new List<string>();
            var currentTaskTypesFromDisk = new List<string>();

            // Check to make sure each task factory is available by its name
            foreach (var taskFactory in taskFactories)
            {
                var cmTaskFactory = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Get_ForName(taskFactory.Name);
                if (cmTaskFactory == null)
                {
                    var newTaskFactoryDto = new CMTaskFactoryDto()
                    {
                        Name = taskFactory.Name,
                        Version = taskFactory.Version
                    };

                    var opResult = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Insert(newTaskFactoryDto);
                    if (opResult.Errors.Any())
                    {
                        return opResult.ErrorsCombined;
                    }
                    cmTaskFactory = CMDataProvider.DataStore.Value.CMTaskFactories.Value.Get_ForName(taskFactory.Name);
                }
                if (currentFactoriesFromDisk.Contains(cmTaskFactory.Name))
                {
                    return $"There is more than 1 task factory registering with the same name {cmTaskFactory.Name}. Please resolve this before running the program.";
                }
                currentFactoriesFromDisk.Add(cmTaskFactory.Name);

                // Make sure all of the task types that this task factory provides are registered in the database
                foreach (var taskTypeName in taskFactory.GetTaskTypes())
                {
                    var cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(taskTypeName);
                    if (cmTaskType == null)
                    {
                        var newTaskTypeDto = new CMTaskTypeDto()
                        {
                            Name = taskTypeName
                        };

                        var opResult = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Insert(newTaskTypeDto);
                        if (opResult.Errors.Any())
                        {
                            return opResult.ErrorsCombined;
                        }
                        cmTaskType = CMDataProvider.DataStore.Value.CMTaskTypes.Value.Get_ForName(taskTypeName);
                    }
                    if (currentTaskTypesFromDisk.Contains(cmTaskType.Name))
                    {
                        return $"There is more than 1 task type registering with the same name {cmTaskType.Name}. Please resolve this before running the program.";
                    }
                    currentTaskTypesFromDisk.Add(cmTaskType.Name);

                    // Make sure the task states for this task type are registered
                    // First make sure the built in states are present
                    var reservedInternalTaskStates = ReservedTaskStates.States;
                    var reservedTaskPluginStates = taskFactory.GetRequiredTaskStates(cmTaskType);
                    var invalidPluginStates = reservedTaskPluginStates.Intersect(reservedInternalTaskStates);
                    if (invalidPluginStates.Any())
                    {
                        var allInvalidStates = string.Join(",", invalidPluginStates);
                        return $"The task factory {taskFactory.Name} is attempting to use reserved state(s) {allInvalidStates}. Please remove this task factory and try again.";
                    }
                    var allReservedTaskStates = reservedInternalTaskStates.Union(reservedTaskPluginStates);
                    int priority = 0;
                    foreach (var taskState in allReservedTaskStates)
                    {
                        var dbTaskState = CMDataProvider.DataStore.Value.CMTaskStates.Value.Get_ForInternalName(taskState, cmTaskType.Id);
                        if (dbTaskState == null)
                        {
                            var newTaskStateDto = new CMTaskStateDto()
                            {
                                DisplayName = taskState,
                                InternalName = taskState,
                                Reserved = true,
                                TaskTypeId = cmTaskType.Id,
                                Priority = ++priority
                            };
                            var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Insert(newTaskStateDto);
                            if (opResult.Errors.Any())
                            {
                                return opResult.ErrorsCombined;
                            }
                        }
                        else
                        {
                            dbTaskState.InternalName = taskState;
                            dbTaskState.DisplayName = taskState;
                            dbTaskState.Reserved = true;
                            var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Update(dbTaskState);
                            if (opResult.Errors.Any())
                            {
                                return opResult.ErrorsCombined;
                            }
                        }
                    }

                    // Un-reserve states that are not required to be reserved now, just in case we are upgrading the db
                    var dbTaskStates = CMDataProvider.DataStore.Value.CMTaskStates.Value.GetAll_ForTaskType(cmTaskType.Id);
                    foreach (var dbTaskState in dbTaskStates)
                    {
                        if (dbTaskState.Reserved && !allReservedTaskStates.Contains(dbTaskState.InternalName))
                        {
                            dbTaskState.Reserved = false;
                            var opResult = CMDataProvider.DataStore.Value.CMTaskStates.Value.Update(dbTaskState);
                            if (opResult.Errors.Any())
                            {
                                return opResult.ErrorsCombined;
                            }
                        }
                    }
                }
            }

            // Go through everything that is currently registered in the db and check for things that are now missing on disk
            // mcbtodo: for now I'm just showing an error here, but there should be a way to either automatically resolve this
            // mcbtodo: issue or give instructions to the user on how to clean up any references to the removed taskfactory/tasktype.


            // mcbtodo: nothing referenced the factories yet, add this back in or delete as some point
            //var dbTaskFactories = CMDataProvider.DataStore.Value.CMTaskFactories.Value.GetAll();
            //foreach (var dbTaskFactory in dbTaskFactories)
            //{
            //    if (!currentFactoriesFromDisk.Contains(dbTaskFactory.Name))
            //    {
            //        return $"Task factory with name {dbTaskFactory.Name} that was previously registered has been removed. Please put this task factory back in place so the program can run properly.";
            //    }
            //}
            var dbTaskTypes = CMDataProvider.DataStore.Value.CMTaskTypes.Value.GetAll();
            foreach (var dbTaskType in dbTaskTypes)
            {
                // The only restriction is that task types are named uniquely across the collection. It is acceptable if a task type moves from one factory to another.
                if (!currentTaskTypesFromDisk.Contains(dbTaskType.Name))
                {
                    return $"Task type {dbTaskType.Name} that was previously registered has been removed. Please restore the previous configuration so the program can run properly.";
                }
            }

            return null;
        }

        private static void TaskFactories_Init()
        {
            var taskFactories = TaskFactoriesCatalog.Instance.TaskFactories.ToList();

            foreach (var taskFactory in taskFactories)
            {
                if (taskFactoryInitialized.ContainsKey(taskFactory.Name))
                {
                    continue;
                }

                taskFactoryInitialized[taskFactory.Name] = true;
                taskFactory.Initialize();
            }
        }

        public static string Init()
        {
            var registerError = RegisterTaskFactories_InDatabase();
            if (!string.IsNullOrWhiteSpace(registerError))
            {
                return registerError;
            }
            TaskFactories_Init();
            return null;
        }

    }
}
