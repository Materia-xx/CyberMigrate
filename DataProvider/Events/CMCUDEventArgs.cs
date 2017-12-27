﻿using System;

namespace DataProvider.Events
{

    public class CMCUDEventArgs : EventArgs
    {
        /// <summary>
        /// The type of operation that was performed
        /// Create, Update or Delete.
        /// </summary>
        public CMCUDActionType ActionType { get; set; }

        /// <summary>
        /// The Dto type that this event is being raised for
        /// </summary>
        public Type DtoType { get; set; }

        /// <summary>
        /// The id of the record that was affected
        /// </summary>
        public int Id { get; set; }
    }
}
