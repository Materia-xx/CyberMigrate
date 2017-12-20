using DataProvider;
using Dto;
using System;

namespace DataProvider
{
    public static class CMDataProvider
    {
        public static Lazy<CMDataProviderMaster> Master = new Lazy<CMDataProviderMaster>(() =>
        {
            return new CMDataProviderMaster();
        });

        public static Lazy<CMDataProviderDataStore> DataStore = new Lazy<CMDataProviderDataStore>(() =>
        {
            // Note this assumes that the data store path is already set up. The program should not access this field until it vierifies this is the case.
            var options = Master.Value.GetOptions();
            return new CMDataProviderDataStore(options.DataStorePath);
        });
    }
}
