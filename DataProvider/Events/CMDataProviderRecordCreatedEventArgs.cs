using System;

namespace DataProvider.Events
{
    public class CMDataProviderRecordCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// The new Dto that was created
        /// </summary>
        public object CreatedDto { get; set; }
    }
}
