using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataProvider.Globals
{
    public static class CMJsonSerializer
    {
        private static JsonSerializerSettings settings;

        public static JsonSerializerSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = new JsonSerializerSettings();
                    settings.Formatting = Formatting.Indented;
                    settings.TypeNameHandling = TypeNameHandling.Auto;
                    settings.Converters.Add(new StringEnumConverter());
                }
                return settings;
            }
        }
    }
}
