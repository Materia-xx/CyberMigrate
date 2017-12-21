﻿using Dto;
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
                s.Name.Equals(systemName, System.StringComparison.OrdinalIgnoreCase)
            );
            return results.FirstOrDefault();
        }
    }
}
