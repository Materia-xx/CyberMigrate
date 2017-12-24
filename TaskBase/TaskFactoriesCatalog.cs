﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace TaskBase
{
    public class TaskFactoriesCatalog
    {
        private CompositionContainer container;

        [ImportMany(typeof(CMTaskFactoryBase))]
        public IEnumerable<CMTaskFactoryBase> TaskFactories { get; set; }

        public static TaskFactoriesCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TaskFactoriesCatalog();
                    instance.InitCatalog();
                }
                return instance;
            }
        }
        private static TaskFactoriesCatalog instance;

        private string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                string directoryPath = Path.GetDirectoryName(path);
                return directoryPath;
            }
        }

        private void InitCatalog()
        {
            var catalog = new AggregateCatalog();

            // Add a catalog that scans the same directory this dll is in for task factories
            catalog.Catalogs.Add(new DirectoryCatalog(AssemblyDirectory));

            // mcbtodo: Also scan {the datastore directory}\Tasks  .. This will provide a way to add user created tasks instead of just builtin

            container = new CompositionContainer(catalog);

            try
            {
                // Start the search and fill in the Tasks property
                container.ComposeParts(this);
            }
            catch //(CompositionException cex)
            {
                // mcbtodo: if there is an error here then the program won't work anyway
                // mcbtodo: might as well blow up with the full and correct stack trace.
                throw;
            }
        }

        private CMTaskFactoryBase GetTaskFactory(string taskTypeName)
        {
            var supportingFactories = TaskFactories.Where(f => f.GetTaskTypes().Contains(taskTypeName));
            if (!supportingFactories.Any())
            {
                throw new InvalidOperationException($"Unable to create a task of type '{taskTypeName}'. The task factory for this task was not found.");
            }
            else if (supportingFactories.Count() > 1)
            {
                throw new InvalidOperationException($"Unable to create a task of type '{taskTypeName}'. There are more than 1 task factories that support this type.");
            }

            return supportingFactories.First();
        }

        public CMTaskBase GetTask(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            var taskFactory = GetTaskFactory(taskTypeName);
            var createdTask = taskFactory.GetTask(taskTypeName, cmSystemId, cmFeatureId, cmTaskId);
            return createdTask;
        }

        /// <summary>
        /// Gets the user control meant to configure the task type
        /// </summary>
        /// <param name="taskTypeName"></param>
        /// <returns></returns>
        public UserControl GetTaskConfigUI(string taskTypeName)
        {
            var taskFactory = GetTaskFactory(taskTypeName);
            var configUC = taskFactory.GetTaskConfigUI(taskTypeName);
            return configUC;
        }

        /// <summary>
        /// Gets the user control meant to edit or view a particular task
        /// </summary>
        /// <param name="taskTypeName"></param>
        /// <param name="cmSystemId"></param>
        /// <param name="cmFeatureId"></param>
        /// <param name="cmTaskId"></param>
        /// <returns></returns>
        public UserControl GetTaskUI(string taskTypeName, int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            var taskFactory = GetTaskFactory(taskTypeName);
            var taskUC = taskFactory.GetTaskUI(taskTypeName, cmSystemId, cmFeatureId, cmTaskId);
            return taskUC;
        }
    }
}
