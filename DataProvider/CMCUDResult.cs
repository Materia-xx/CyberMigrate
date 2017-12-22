using System;
using System.Collections.Generic;

namespace DataProvider
{
    /// <summary>
    /// Represents the result of Create, Update and Delete operations done with the CRUD providers
    /// </summary>
    public class CMCUDResult
    {
        /// <summary>
        /// If there are any errors present then it can be considered that the operation was not a success
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Combines all errors into 1 string.
        /// </summary>
        public string ErrorsCombined => string.Join(Environment.NewLine, Errors);
    }
}
