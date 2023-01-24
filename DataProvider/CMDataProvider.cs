using DataProvider.ProgramConfig;
using LiteDB;
using System;
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

        internal static LiteDatabase DataStoreDatabase
        {
            get
            {
                if (dataStoreDatabase == null)
                {
                    var options = CMProgramConfig.ReadLocalAppData();

                    var dataStoreDbPath = Path.Combine(options.DataStorePath, "CyberMigrate.db");
                    dataStoreDatabase = new LiteDatabase(dataStoreDbPath);
                }
                return dataStoreDatabase;
            }
        }
        private static LiteDatabase dataStoreDatabase;

        public static Lazy<CMDataProviderDataStore> DataStore = new Lazy<CMDataProviderDataStore>(() =>
        {
            // Note this assumes that the data store path is already set up. The program should not access this field until it verifies this is the case.
            var options = CMProgramConfig.ReadLocalAppData();
            return new CMDataProviderDataStore(DataStoreDatabase);
        });
    }
}
