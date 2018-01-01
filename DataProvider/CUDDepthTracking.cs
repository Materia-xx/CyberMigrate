namespace DataProvider
{
    public static class CUDDepthTracking
    {
        /// <summary>
        /// Keeps track of the depth of Create, Update and Delete operations.
        /// In multiple places in the program the Created, Updated and Deleted events are subscribed to and in-turn
        /// do additional CUD operations, which in-turn may chain into more, etc. This is also added to by task libraris
        /// that can also subscribe to events. As protection against stack overflows caused by incorrectly coded
        /// routines, missing protections, etc, the depth of all CUD operations is tracked through this variable.
        /// The program will return an error through the <see cref="CMCUDResult"/> when the configured limit is exceeded.
        /// </summary>
        public static int OperationDepth { get; set; } = 0;

        /// <summary>
        /// The max operation depth allowed before a Create, Update or Delete operation will return an error.
        /// </summary>
        private static int OperationMaxDepth { get; set; } = 10; // mcbtodo: make a config option for this.

        public static bool ExceedsMaxOperationDepth(CMCUDResult opResult)
        {
            if (OperationDepth > OperationMaxDepth)
            {
                opResult.Errors.Add($"Create, Update or Delete operations have exceeded the max depth of {OperationMaxDepth}.");
                return true;
            }
            return false;
        }
    }
}
