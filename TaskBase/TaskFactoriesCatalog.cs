using Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Windows.Controls;

namespace TaskBase
{
    public class TaskFactoriesCatalog
    {
        private CompositionContainer container;


        [ImportMany(typeof(CMTaskFactoryBase))]
        public IEnumerable<CMTaskFactoryBase> TaskFactories { get; set; }

        public Dictionary<string, CMTaskFactoryBase> TaskFactoriesByTaskType { get; set; } = new Dictionary<string, CMTaskFactoryBase>();

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

            foreach (var taskFactory in TaskFactories)
            {
                foreach (var taskType in taskFactory.GetTaskTypes())
                {
                    if (TaskFactoriesByTaskType.ContainsKey(taskType))
                    {
                        throw new InvalidOperationException($"Unable to register task of type '{taskType}'. There are more than 1 task factories that support this type.");
                    }

                    TaskFactoriesByTaskType[taskType] = taskFactory;
                }
            }
        }

        private CMTaskFactoryBase GetTaskFactory(string taskTypeName)
        {
            if (!TaskFactoriesByTaskType.ContainsKey(taskTypeName))
            {
                throw new InvalidOperationException($"Unable to create a task of type '{taskTypeName}'. The task factory for this task was not found.");
            }
            return TaskFactoriesByTaskType[taskTypeName];
        }

        /// <summary>
        /// Gets the user control meant to configure the task type
        /// </summary>
        /// <param name="cmTaskType"></param>
        /// <returns></returns>
        public UserControl GetTaskConfigUI(CMTaskTypeDto cmTaskType)
        {
            var taskFactory = GetTaskFactory(cmTaskType.Name);
            var configUC = taskFactory.GetTaskConfigUI(cmTaskType);
            return configUC;
        }

        /// <summary>
        /// Gets the user control meant to edit or view a particular task
        /// </summary>
        /// <param name="cmTaskType"></param>
        /// <param name="cmSystem"></param>
        /// <param name="cmFeature"></param>
        /// <param name="cmTask"></param>
        /// <returns></returns>
        public UserControl GetTaskUI(CMTaskTypeDto cmTaskType, CMSystemDto cmSystem, CMFeatureDto cmFeature, CMTaskDto cmTask)
        {
            var taskFactory = GetTaskFactory(cmTaskType.Name);
            var taskUC = taskFactory.GetTaskUI(cmTaskType, cmSystem, cmFeature, cmTask);
            return taskUC;
        }

        /// <summary>
        /// Called when instancing a task. A <see cref="CMTaskDto"/> will have already been created and is passed in.
        /// The task factory should create any task data for this new task and take care of updating the database.
        /// </summary>
        /// <param name="cmTaskType"></param>
        /// <param name="cmTaskInstance">The id of the newly created CMTaskDto instance that was created from the template</param>
        public void CreateTaskDataInstance(CMTaskTypeDto cmTaskType, CMTaskDto cmTaskTemplate, CMTaskDto cmTaskInstance)
        {
            var taskFactory = GetTaskFactory(cmTaskType.Name);
            taskFactory.CreateTaskDataInstance(cmTaskType, cmTaskTemplate, cmTaskInstance);
        }

        public bool HasTaskData(CMTaskTypeDto cmTaskType, CMTaskDto cmTask)
        {
            var taskFactory = GetTaskFactory(cmTaskType.Name);
            return taskFactory.HasTaskData(cmTaskType, cmTask);
        }
    }
}