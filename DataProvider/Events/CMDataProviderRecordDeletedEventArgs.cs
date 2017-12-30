using System;

namespace DataProvider.Events
{

    public class CMDataProviderRecordDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// The Dto as it was before being deleted
        /// </summary>
        public object DtoBefore { get; set;  }
    }
}
