using Dto;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProvider
{
    /// <summary>
    /// A data provider that interacts with the CyberMigrate master database.
    /// This database is intended only as a store for program options and is always stored
    /// in the same folder that the program is running from.
    /// </summary>
    public class CMDataProviderMaster
    {
        private string programDirectory;
        private string programDbPath;

        public CMDataProviderMaster()
        {
            programDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            programDbPath = Path.Combine(programDirectory, "CyberMigrateMaster.db");
        }

        /// <summary>
        /// The program can only have one data store. If it has been configured in the program than this will return that path.
        /// </summary>
        /// <returns></returns>
        public CMOptions GetOptions()
        {
            using (var db = new LiteDatabase(programDbPath))
            {
                var optionsCollection = db.GetCollection<CMOptions>("CyberMigrateOptions");
                var options = optionsCollection.FindAll().FirstOrDefault();

                if (options == null)
                {
                    options = new CMOptions();
                    optionsCollection.Insert(options);
                }

                return options;
            }
        }

        public void updateOptions(CMOptions options)
        {
            using (var db = new LiteDatabase(programDbPath))
            {
                var optionsCollection = db.GetCollection<CMOptions>("CyberMigrateOptions");
                if (!optionsCollection.Update(options))
                {
                    throw new InvalidOperationException("Options were not found in master db.");
                }
            }
        }

        // mcbtodo: just keeping these around for example. delete when there is other examples like this

        ///// <summary>
        ///// Adds a new data store location for the program to track. 
        ///// A data store is a complete, isolated set of systems, features and tasks.
        ///// </summary>
        ///// <param name="dataStorePath"></param>
        //public bool AddDataStore(DataStore store)
        //{
        //    using (var db = new LiteDatabase(programDbPath))
        //    {
        //        var dataStores = db.GetCollection<DataStore>("DataStores");

        //        // First search for an already existing entry
        //        var results = dataStores.Find(ds => ds.StorePath.Equals(store.StorePath, StringComparison.OrdinalIgnoreCase));
        //        if (results.Any())
        //        {
        //            return false;
        //        }

        //        // Add it
        //        dataStores.Insert(store);
        //        return true;
        //    }
        //}

        //public IEnumerable<DataStore> GetDataStores()
        //{
        //    using (var db = new LiteDatabase(programDbPath))
        //    {
        //        var dataStores = db.GetCollection<DataStore>("DataStores");

        //        return dataStores.FindAll();
        //    }
        //}

    }
}
