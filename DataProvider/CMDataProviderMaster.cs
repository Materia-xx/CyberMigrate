using Dto;
using LiteDB;
using System;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// A data provider that interacts with the CyberMigrate master database.
    /// This database is intended only as a store for program options and is always stored
    /// in the same folder that the program is running from.
    /// </summary>
    public class CMDataProviderMaster
    {
        private LiteDatabase db;
        private LiteCollection<CMOptionsDto> optionsCollection;

        public CMDataProviderMaster(LiteDatabase masterDatabase)
        {
            this.db = masterDatabase;
            this.optionsCollection = db.GetCollection<CMOptionsDto>("CyberMigrateOptions");
        }

        /// <summary>
        /// The program can only have one data store. If it has been configured in the program than this will return that path.
        /// </summary>
        /// <returns></returns>
        public CMOptionsDto GetOptions()
        {
            var options = optionsCollection.FindAll().FirstOrDefault();

            if (options == null)
            {
                options = new CMOptionsDto();
                optionsCollection.Insert(options);
            }

            return options;
        }

        public void UpdateOptions(CMOptionsDto options)
        {
            if (!optionsCollection.Update(options))
            {
                throw new InvalidOperationException("Options were not found in master db.");
            }
        }
    }
}
