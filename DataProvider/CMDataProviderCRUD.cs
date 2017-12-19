using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// Provides a base of C.R.U.D. operations for a table that has a single id and no foreign keys in the CyberMigrate database.
    /// Create, Read, Update, Delete
    /// </summary>
    public class CMDataProviderCRUD<T> where T : IdBasedObject
    {
        private string cmDatabasePath;
        private string collectionName;

        public CMDataProviderCRUD(string cmDatabasePath, string collectionName)
        {
            this.cmDatabasePath = cmDatabasePath;
            this.collectionName = collectionName;
        }

        public virtual IEnumerable<T> GetAll()
        {
            using (var db = new LiteDatabase(cmDatabasePath))
            {
                var cmCollection = db.GetCollection<T>(collectionName);
                return cmCollection.FindAll();
            }
        }

        public virtual T Get(int id)
        {
            using (var db = new LiteDatabase(cmDatabasePath))
            {
                var cmCollection = db.GetCollection<T>(collectionName);

                return cmCollection.Find(s => s.Id == id).FirstOrDefault();
            }
        }

        public virtual bool Upsert(T updatingObject)
        {
            using (var db = new LiteDatabase(cmDatabasePath))
            {
                var cmCollection = db.GetCollection<T>(collectionName);
                var existingCMSystem = cmCollection.Find(s => s.Id == updatingObject.Id);

                if (existingCMSystem.Any())
                {
                    cmCollection.Update(updatingObject);
                }
                else
                {
                    cmCollection.Insert(updatingObject);
                }
                
                return true;
            }
        }

        public virtual bool Delete(int deletingId)
        {
            using (var db = new LiteDatabase(cmDatabasePath))
            {
                var cmCollection = db.GetCollection<T>(collectionName);
                return cmCollection.Delete(deletingId);
            }
        }
    }
}
