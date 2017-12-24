using Dto;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataProvider
{
    public static class CMDataProvider
    {
        public static string ProgramExeFolder
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            }
        }

        private static Dictionary<Type, object> TaskTypeDataProviders = new Dictionary<Type, object>();

        private static LiteDatabase DataStoreDatabase
        {
            get
            {
                if (dataStoreDatabase == null)
                {
                    var options = Master.Value.GetOptions();

                    var dataStoreDbPath = Path.Combine(options.DataStorePath, "CyberMigrate.db");
                    dataStoreDatabase = new LiteDatabase(dataStoreDbPath);
                }
                return dataStoreDatabase;
            }
        }
        private static LiteDatabase dataStoreDatabase;

        public static Lazy<CMDataProviderMaster> Master = new Lazy<CMDataProviderMaster>(() =>
        {
            var programDbPath = Path.Combine(ProgramExeFolder, "CyberMigrateMaster.db");
            var masterDatabase = new LiteDatabase(programDbPath);

            return new CMDataProviderMaster(masterDatabase);
        });

        public static Lazy<CMDataProviderDataStore> DataStore = new Lazy<CMDataProviderDataStore>(() =>
        {
            // Note this assumes that the data store path is already set up. The program should not access this field until it verifies this is the case.
            var options = Master.Value.GetOptions();
            return new CMDataProviderDataStore(DataStoreDatabase);
        });

        /// <summary>
        /// Gets a data provider linked to the DTO type specified by type T.
        /// Meant for creating up generic providers within the context of a task, and for storing task specific data.
        /// </summary>
        /// <typeparam name="T">A Dto type that inherits from <see cref="CMTaskDataDtoBase"/></typeparam>
        /// <returns></returns>
        public static CMTaskDataCRUD<T> GetTaskTypeDataProvider<T>() where T : CMTaskDataDtoBase
        {
            if (!TaskTypeDataProviders.ContainsKey(typeof(T)))
            {
                var typeName = typeof(T).Name;
                var collectionName = $"TaskData_{typeName}";
                var newDataProvider = new CMTaskDataCRUD<T>(DataStoreDatabase, collectionName);
                TaskTypeDataProviders.Add(typeof(T), newDataProvider);
            }

            var dataProviderObj = TaskTypeDataProviders[typeof(T)];
            var dataProvider = dataProviderObj as CMTaskDataCRUD<T>;
            return dataProvider;
        }
    }
}
