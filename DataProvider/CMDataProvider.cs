using CyberMigrateCommom;
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

        public SingletonWrapper<CMSystemsCRUD> CMSystems { get; set; }

        public SingletonWrapper<CMSystemStatesCRUD> CMSystemStates { get; set; }

        public SingletonWrapper<CMFeatureTemplatesCRUD> CMFeatureTemplates { get; set; }

        public SingletonWrapper<CMFeatureStateTransitionRulesCRUD> CMFeatureStateTransitionRules { get; set; }

        public CMDataProvider(string dataStoreDirectory)
        {
            this.dataStoreDirectory = dataStoreDirectory;
            dataStoreDbPath = Path.Combine(dataStoreDirectory, "CyberMigrate.db");

            CMSystems = new SingletonWrapper<CMSystemsCRUD>(() =>
            {
                return new CMSystemsCRUD(dataStoreDbPath, "Systems");
            });

            CMSystemStates = new SingletonWrapper<CMSystemStatesCRUD>(() =>
            {
                return new CMSystemStatesCRUD(dataStoreDbPath, "SystemStates");
            });

            CMFeatureTemplates = new SingletonWrapper<CMFeatureTemplatesCRUD>(() =>
            {
                return new CMFeatureTemplatesCRUD(dataStoreDbPath, "FeatureTemplates");
            });

            CMFeatureStateTransitionRules = new SingletonWrapper<CMFeatureStateTransitionRulesCRUD>(() =>
            {
                return new CMFeatureStateTransitionRulesCRUD(dataStoreDbPath, "FeatureStateTransitionRules");
            });
        }
    }
}
