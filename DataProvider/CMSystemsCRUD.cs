using Dto;
using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace DataProvider
{
    /// <summary>
    /// Provides additional functions for interacting with the Systems table that are not provided in the base CRUD class
    /// </summary>
    public class CMSystemsCRUD : CMDataProviderCRUDBase<CMSystemDto>
    {
        public CMSystemsCRUD(LiteDatabase liteDatabase, string collectionName) : base(liteDatabase, collectionName)
        {
        }

        /// <summary>
        /// Returns all systems in the datastore
        /// </summary>
        /// <returns></returns>
        public new IEnumerable<CMSystemDto> GetAll()
        {
            var results = base.GetAll();
            return results.OrderBy(s => s.Name);
        }

        /// <summary>
        /// Returns the first system with the given name or null
        /// </summary>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public CMSystemDto Get_ForSystemName(string systemName)
        {
            var results = Find(s =>
                s.Name.Equals(systemName, System.StringComparison.Ordinal)  // Sensitive case allows the user to more easily rename items by just the case
            );
            return results.FirstOrDefault();
        }

        public override CMCUDResult Insert(CMSystemDto insertingObject)
        {
            var opResult = new CMCUDResult();

            if (Get_ForSystemName(insertingObject.Name) != null)
            {
                opResult.Errors.Add($"A system with the name '{insertingObject.Name}' already exists. Rename that one first.");
                return opResult;
            }

            return base.Insert(insertingObject);
        }

        public override CMCUDResult Update(CMSystemDto updatingObject)
        {
            var opResult = new CMCUDResult();

            if (Get_ForSystemName(updatingObject.Name) != null)
            {
                opResult.Errors.Add($"A system with the name '{updatingObject.Name}' already exists.");
                return opResult;
            }

            return base.Update(updatingObject);
        }

        // mcbtodo: add a delete override that does not allow deletes of systems that currently have features of any type

    }
}
