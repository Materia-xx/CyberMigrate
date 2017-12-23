using LiteDB;
using System;

namespace DataProvider
{
    /// <summary>
    /// A data provider that interacts with the CyberMigrate data store database.
    /// </summary>
    public class CMDataProviderDataStore
    {
        public Lazy<CMSystemsCRUD> CMSystems { get; set; }

        public Lazy<CMSystemStatesCRUD> CMSystemStates { get; set; }

        public Lazy<CMFeaturesCRUD> CMFeatures { get; set; }

        public Lazy<CMFeatureStateTransitionRulesCRUD> CMFeatureStateTransitionRules { get; set; }

        public Lazy<CMTasksCRUD> CMTasks { get; set; }

        public Lazy<CMTaskFactoriesCRUD> CMTaskFactories { get; set; }

        public Lazy<CMTaskTypesCRUD> CMTaskTypes { get; set; }

        public Lazy<CMTaskStatesCRUD> CMTaskStates { get; set; }

        public CMDataProviderDataStore(LiteDatabase dataStoreDatabase)
        {
            CMSystems = new Lazy<CMSystemsCRUD>(() =>
            {
                return new CMSystemsCRUD(dataStoreDatabase, "Systems");
            });

            CMSystemStates = new Lazy<CMSystemStatesCRUD>(() =>
            {
                return new CMSystemStatesCRUD(dataStoreDatabase, "SystemStates");
            });

            CMFeatures = new Lazy<CMFeaturesCRUD>(() =>
            {
                return new CMFeaturesCRUD(dataStoreDatabase, "Features");
            });

            CMFeatureStateTransitionRules = new Lazy<CMFeatureStateTransitionRulesCRUD>(() =>
            {
                return new CMFeatureStateTransitionRulesCRUD(dataStoreDatabase, "FeatureStateTransitionRules");
            });

            CMTasks = new Lazy<CMTasksCRUD>(() =>
            {
                return new CMTasksCRUD(dataStoreDatabase, "Tasks");
            });

            CMTaskFactories = new Lazy<CMTaskFactoriesCRUD>(() =>
            {
                return new CMTaskFactoriesCRUD(dataStoreDatabase, "TaskFactories");
            });

            CMTaskTypes = new Lazy<CMTaskTypesCRUD>(() =>
            {
                return new CMTaskTypesCRUD(dataStoreDatabase, "TaskTypes");
            });

            CMTaskStates = new Lazy<CMTaskStatesCRUD>(() =>
            {
                return new CMTaskStatesCRUD(dataStoreDatabase, "TaskStates");
            });
        }
    }
}
