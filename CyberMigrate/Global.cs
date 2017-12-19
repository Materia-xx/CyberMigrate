using DataProvider;
using Dto;
using System;

namespace CyberMigrate
{
    public static class Global
    {
        public static Lazy<CMDataProviderMaster> CmMasterDataProvider = new Lazy<CMDataProviderMaster>(() =>
        {
            return new CMDataProviderMaster();
        });

        public static Lazy<CMDataProvider> CmDataProvider = new Lazy<CMDataProvider>(() =>
        {
            // Note this assumes that the data store path is already set up. The program should not access this field until it vierifies this is the case.
            var options = CmMasterDataProvider.Value.GetOptions();
            return new CMDataProvider(options.DataStorePath);
        });
    }
}
