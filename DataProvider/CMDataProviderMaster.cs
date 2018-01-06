using DataProvider.Events;
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
        public CMDataProviderRecordDeletedEvent OnRecordDeleted;

        public CMDataProviderMaster(LiteDatabase masterDatabase)
        {
            this.db = masterDatabase;
            this.optionsCollection = db.GetCollection<CMOptionsDto>("CyberMigrateOptions");
        }

        /// <summary>
        /// Deletes the options from the database
        /// </summary>
        /// <returns></returns>
        public CMCUDResult DeleteOptions()
        {
            var opResult = new CMCUDResult();

            var options = GetOptions();

            OnRecordDeleted?.Invoke(
                new CMDataProviderRecordDeletedEventArgs()
                {
                    DtoBefore = options,
                });

            if (!optionsCollection.Delete(options.Id))
            {
                opResult.Errors.Add($"Option with id {options.Id} was not found to delete.");
            }

            return opResult;
        }

        /// <summary>
        /// The program can only have one data store. If it has been configured in the program than this will return that path.
        /// </summary>
        /// <returns></returns>
        public CMOptionsDto GetOptions()
        {
            var allOptions = optionsCollection.FindAll();
            var options = allOptions.FirstOrDefault();

            if (options == null)
            {
                options = new CMOptionsDto();
                optionsCollection.Insert(options);

                OnRecordCreated?.Invoke(
                    new CMDataProviderRecordCreatedEventArgs()
                    {
                        CreatedDto = options
                    });
            }

            return options;
        }

        public CMCUDResult UpdateOptions(CMOptionsDto options)
        {
            var opResult = new CMCUDResult();

            var updateEvent = new CMDataProviderRecordUpdatedEventArgs()
            {
                DtoBefore = GetOptions(),
                DtoAfter = options,
            };

            if (!optionsCollection.Update(options))
            {
                opResult.Errors.Add("Options were not found in master db.");
                return opResult;
            }

            OnRecordUpdated?.Invoke(updateEvent);

            return opResult;
        }
    }
}
