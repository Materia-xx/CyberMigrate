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
        protected string CollectionName { get; set; }
        private LiteCollection<T> cmCollection;

        public CMDataProviderCRUDBase(LiteDatabase liteDatabase, string collectionName)
        {
            this.db = liteDatabase;
            this.CollectionName = collectionName;
            cmCollection = db.GetCollection<T>(collectionName);
        }

        /// <summary>
        /// Returns all elements of the collection.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<T> GetAll()
        {
            var results = cmCollection.FindAll();
            return results;
        }

        protected virtual IEnumerable<T> Find(Expression<Func<T,bool>> expression)
        {
            var results = cmCollection.Find(expression);
            return results;
        }

        protected virtual int Count(Expression<Func<T, bool>> expression)
        {
            var results = cmCollection.Count(expression);
            return results;
        }

        public virtual T Get(int id)
        {
            return cmCollection.Find(s => s.Id == id).FirstOrDefault();
        }

        public virtual CMCUDResult Insert(T insertingObject)
        {
            var opResult = new CMCUDResult();
            if (insertingObject.Id != 0)
            {
                opResult.Errors.Add($"Cannot insert a new item into {CollectionName}. New items must have their id set to 0 before insert.");
                return opResult;
            }

            cmCollection.Insert(insertingObject);
            return opResult;
        }

        public virtual CMCUDResult Update(T updatingObject)
        {
            var opResult = new CMCUDResult();
            if (cmCollection.Update(updatingObject) == false)
            {
                opResult.Errors.Add($"An item in {CollectionName} with id {updatingObject.Id} was not found to update.");
            }
            return opResult;
        }

        public virtual CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();

            if (!cmCollection.Delete(deletingId))
            {
                opResult.Errors.Add($"{CollectionName} with id {deletingId} was not found to delete.");
            }

            return opResult;
        }
    }
}
