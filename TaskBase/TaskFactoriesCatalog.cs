using System;
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

        public CMTaskBase CreateTask(string taskName, int cmSystemId, int cmFeatureId, int cmTaskId)
        {
            var supportingFactories = TaskFactories.Where(f => f.SupportedTasks.Contains(taskName));
            if (!supportingFactories.Any())
            {
                throw new InvalidOperationException($"Unable to create a task of type '{taskName}'. The task factory for this task was not found.");
            }
            else if (supportingFactories.Count() > 1)
            {
                throw new InvalidOperationException($"Unable to create a task of type '{taskName}'. There are more than 1 task factories that support this type.");
            }

            var createdTask = supportingFactories.First().CreateTask(cmSystemId, cmFeatureId, cmTaskId);
            return createdTask;
        }

        public UserControl GetConfigUI(string taskFactoryName)
        {
            var matchingFactories = TaskFactories.Where(f => f.GetType().Name.Equals(taskFactoryName, StringComparison.OrdinalIgnoreCase));

            if (!matchingFactories.Any())
            {
                throw new InvalidOperationException($"Unable to create a config UI for '{taskFactoryName}'. The task factory was not found.");
            }
            else if (matchingFactories.Count() > 1)
            {
                throw new InvalidOperationException($"Unable to create a config UI for '{taskFactoryName}'. There are more than 1 task factories with this name.");
            }

            var configUI = matchingFactories.First().GetConfigurationUI();
            return configUI;
        }

    }
}
