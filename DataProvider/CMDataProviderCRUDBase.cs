using Dto;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DataProvider
{
    /// <summary>
    /// Provides a base of C.R.U.D. operations for a table that has a single id and no foreign keys in the CyberMigrate database.
    /// Create, Read, Update, Delete
    /// </summary>
    public abstract class CMDataProviderCRUDBase<T> where T : IdBasedObject
    {
        private LiteDatabase db;
        private string collectionName;
        private LiteCollection<T> cmCollection;

        public CMDataProviderCRUDBase(LiteDatabase liteDatabase, string collectionName)
        {
            this.db = liteDatabase;
            this.collectionName = collectionName;
            cmCollection = db.GetCollection<T>(collectionName);
        }

        /// <summary>
        /// Returns all elements of the collection.
        /// Use <see cref="Find(Query)"/> instead if the intention is the filter the returned results.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<T> GetAll()
        {
            var results = cmCollection.FindAll();
            return results;
        }

        protected virtual IEnumerable<T> Find(Expression<Func<T,bool>> expression)
        {
            var results = cmCollection.Find(expression);
            return results;
        }

        public virtual T Get(int id)
        {
            return cmCollection.Find(s => s.Id == id).FirstOrDefault();
        }

        public virtual bool Upsert(T updatingObject)
        {
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

        public virtual bool Delete(int deletingId)
        {
            return cmCollection.Delete(deletingId);
        }
    }
}
