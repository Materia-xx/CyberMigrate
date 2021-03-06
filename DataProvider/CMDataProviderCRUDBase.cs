﻿using DataProvider.Events;
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

        public delegate void CMDataProviderRecordCreatedEvent(CMDataProviderRecordCreatedEventArgs createdRecordEventArgs);
        public delegate void CMDataProviderRecordUpdatedEvent(CMDataProviderRecordUpdatedEventArgs updatedRecordEventArgs);
        public delegate void CMDataProviderRecordDeletedEvent(CMDataProviderRecordDeletedEventArgs deletedRecordEventArgs);

        /// <summary>
        /// Raised after a record is created and inserted into the database.
        /// The object passed in the event args will have the newly inserted Id.
        /// </summary>
        public CMDataProviderRecordCreatedEvent OnRecordCreated;

        /// <summary>
        /// Raised after a record has been updated in the database.
        /// The objects in the event args will represent both the before and after state of the affected Dto.
        /// </summary>
        public CMDataProviderRecordUpdatedEvent OnRecordUpdated;

        /// <summary>
        /// Raised just before the delete operation is performed.
        /// The object passed in the event args will the the record that will be deleted.
        /// </summary>
        public CMDataProviderRecordDeletedEvent OnBeforeRecordDeleted;

        /// <summary>
        /// Raised after the delete operation is performed.
        /// The object passed in the event args will the the record that was deleted.
        /// </summary>
        public CMDataProviderRecordDeletedEvent OnAfterRecordDeleted;

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

        /// <summary>
        /// Gets all results added to a dictionary where the key is the record id
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<int, T> GetAll_AsLookup()
        {
            return GetAll()
                .ToDictionary(v => v.Id, v => v);
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
            if (CUDDepthTracking.ExceedsMaxOperationDepth(opResult))
            {
                return opResult;
            }

            if (insertingObject.Id != 0)
            {
                opResult.Errors.Add($"Cannot insert a new item into {CollectionName}. New items must have their id set to 0 before insert.");
                return opResult;
            }

            cmCollection.Insert(insertingObject);

            try
            {
                CUDDepthTracking.OperationDepth++;
                OnRecordCreated?.Invoke(
                    new CMDataProviderRecordCreatedEventArgs()
                    {
                        CreatedDto = insertingObject
                    });
            }
            catch (Exception ex)
            {
                opResult.Errors.Add(ex.ToString());
            }
            finally
            {
                CUDDepthTracking.OperationDepth--;
            }
            return opResult;
        }

        public virtual CMCUDResult Update(T updatingObject)
        {
            var opResult = new CMCUDResult();
            if (CUDDepthTracking.ExceedsMaxOperationDepth(opResult))
            {
                return opResult;
            }

            var updateEvent = new CMDataProviderRecordUpdatedEventArgs()
            {
                DtoBefore = Get(updatingObject.Id),
                DtoAfter = updatingObject,
            };

            if (cmCollection.Update(updatingObject) == false)
            {
                opResult.Errors.Add($"An item in {CollectionName} with id {updatingObject.Id} was not found to update.");
                return opResult;
            }

            try
            {
                CUDDepthTracking.OperationDepth++;
                OnRecordUpdated?.Invoke(updateEvent);
            }
            catch (Exception ex)
            {
                opResult.Errors.Add(ex.ToString());
            }
            finally
            {
                CUDDepthTracking.OperationDepth--;
            }

            return opResult;
        }

        public virtual CMCUDResult Delete(int deletingId)
        {
            var opResult = new CMCUDResult();
            if (CUDDepthTracking.ExceedsMaxOperationDepth(opResult))
            {
                return opResult;
            }

            var dtoBefore = Get(deletingId);

            try
            {
                CUDDepthTracking.OperationDepth++;
                OnBeforeRecordDeleted?.Invoke(
                    new CMDataProviderRecordDeletedEventArgs()
                    {
                        DtoBefore = dtoBefore,
                    });
            }
            catch (Exception ex)
            {
                opResult.Errors.Add(ex.ToString());
            }
            finally
            {
                CUDDepthTracking.OperationDepth--;
            }

            if (!cmCollection.Delete(deletingId))
            {
                opResult.Errors.Add($"{CollectionName} with id {deletingId} was not found to delete.");
            }

            try
            {
                CUDDepthTracking.OperationDepth++;
                OnAfterRecordDeleted?.Invoke(
                    new CMDataProviderRecordDeletedEventArgs()
                    {
                        DtoBefore = dtoBefore,
                    });
            }
            catch (Exception ex)
            {
                opResult.Errors.Add(ex.ToString());
            }
            finally
            {
                CUDDepthTracking.OperationDepth--;
            }

            return opResult;
        }
    }
}
