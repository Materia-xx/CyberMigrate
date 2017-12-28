using System.Collections.Generic;

namespace DataProvider
{
    public static class ReservedTaskStates
    {
        public const string Closed = "Closed";
        public const string Template = "Template";
        public const string Instance = "Instance";

        public static List<string> States { get; set; } = new List<string>()
        {
            Template,
            Instance,
            Closed
        };
    }
}
