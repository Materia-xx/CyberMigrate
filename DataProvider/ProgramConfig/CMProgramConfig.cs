using DataProvider.Globals;
using Dto;
using Newtonsoft.Json;
using System;
using System.IO;

namespace DataProvider.ProgramConfig
{
    public static class CMProgramConfig
    {
        private static readonly string appDataFilename = "options.json";

        public static DirectoryInfo GetAppDataFolder()
        {
            var gemAppDataFolder = new DirectoryInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CymberMigrate"));
            if (!gemAppDataFolder.Exists)
            {
                gemAppDataFolder.Create();
            }
            return gemAppDataFolder;
        }

        public static bool WriteLocalAppData(CMOptionsDto options)
        {
            var appdataFolder = GetAppDataFolder();

            // Write the metadata
            try
            {
                var gemLocalAppDataFile = new FileInfo(Path.Combine(appdataFolder.FullName, appDataFilename));
                var gemLocalAppDataJson = JsonConvert.SerializeObject(options, CMJsonSerializer.Settings);
                File.WriteAllText(gemLocalAppDataFile.FullName, gemLocalAppDataJson);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static CMOptionsDto ReadLocalAppData()
        {
            var appDataFolder = GetAppDataFolder();

            try
            {
                var appDataFile = new FileInfo(Path.Combine(appDataFolder.FullName, appDataFilename));
                if (!appDataFile.Exists)
                {
                    // If the local app data doesn't exist then don't default it. The user needs to update the settings.
                    return null;
                }
                else
                {
                    var appDataJson = File.ReadAllText(appDataFile.FullName);
                    var appData = JsonConvert.DeserializeObject<CMOptionsDto>(appDataJson, CMJsonSerializer.Settings);
                    return appData;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
