using CyberMigrateCommom;
using DataProvider;
using Dto;

namespace CyberMigrate
{
    public static class Global
    {
        public static SingletonWrapper<CMDataProviderMaster> CmMasterDataProvider = new SingletonWrapper<CMDataProviderMaster>(() =>
        {
            return new CMDataProviderMaster();
        });

        public static SingletonWrapper<CMDataProvider> CmDataProvider = new SingletonWrapper<CMDataProvider>(() =>
        {
            // Note this assumes that the data store path is already set up. The program should not access this field until it vierifies this is the case.
            var options = CmMasterDataProvider.Instance.GetOptions();
            return new CMDataProvider(options.DataStorePath);
        });
    }
}
