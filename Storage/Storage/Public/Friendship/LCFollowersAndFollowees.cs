using System.Collections.Generic;

namespace LeanCloud.Storage {
    /// <summary>
    /// LCFollowersAndFollowees contains followers and followees.
    /// </summary>
    public class LCFollowersAndFollowees {
        /// <summary>
        /// The followers.
        /// </summary>
        public List<LCObject> Followers {
            get; internal set;
        }

        /// <summary>
        /// The followees.
        /// </summary>
        public List<LCObject> Followees {
            get; internal set;
        }

        /// <summary>
        /// The count of followers.
        /// </summary>
        public int FollowersCount {
            get; internal set;
        }

        /// <summary>
        /// The count of followees.
        /// </summary>
        public int FolloweesCount {
            get; internal set;
        }
    }
}
