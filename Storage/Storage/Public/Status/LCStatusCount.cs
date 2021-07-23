namespace LeanCloud.Storage {
    /// <summary>
    /// LCStatusCount is a result that contains the count of status.
    /// </summary>
    public class LCStatusCount {
        /// <summary>
        /// The total count.
        /// </summary>
        public int Total {
            get; set;
        }

        /// <summary>
        /// The unread count.
        /// </summary>
        public int Unread {
            get; set;
        }
    }
}
