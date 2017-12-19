using System;
using System.IO;

namespace DataProvider
{
    /// <summary>
    /// A data provider that interacts with the CyberMigrate data store database.
    /// </summary>
    public class CMDataProvider
    {
        private string dataStoreDirectory;
        private string dataStoreDbPath;

        //public SingletonWrapper<CMSystemsCRUD> CMSystems { get; set; }
        public Lazy<CMSystemsCRUD> CMSystems { get; set; }

        public Lazy<CMSystemStatesCRUD> CMSystemStates { get; set; }

        public Lazy<CMFeatureTemplatesCRUD> CMFeatureTemplates { get; set; }

        public Lazy<CMFeatureStateTransitionRulesCRUD> CMFeatureStateTransitionRules { get; set; }

        public CMDataProvider(string dataStoreDirectory)
        {
            this.dataStoreDirectory = dataStoreDirectory;
            dataStoreDbPath = Path.Combine(dataStoreDirectory, "CyberMigrate.db");

            CMSystems = new Lazy<CMSystemsCRUD>(() =>
            {
                return new CMSystemsCRUD(dataStoreDbPath, "Systems");
            });

            CMSystemStates = new Lazy<CMSystemStatesCRUD>(() =>
            {
                return new CMSystemStatesCRUD(dataStoreDbPath, "SystemStates");
            });

            CMFeatureTemplates = new Lazy<CMFeatureTemplatesCRUD>(() =>
            {
                return new CMFeatureTemplatesCRUD(dataStoreDbPath, "FeatureTemplates");
            });

            CMFeatureStateTransitionRules = new Lazy<CMFeatureStateTransitionRulesCRUD>(() =>
            {
                return new CMFeatureStateTransitionRulesCRUD(dataStoreDbPath, "FeatureStateTransitionRules");
            });
        }
    }
}
