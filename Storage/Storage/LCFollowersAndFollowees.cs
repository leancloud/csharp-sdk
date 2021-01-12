using System.Collections.Generic;

namespace LeanCloud.Storage {
    public class LCFollowersAndFollowees {
        public List<LCObject> Followers {
            get; internal set;
        }

        public List<LCObject> Followees {
            get; internal set;
        }

        public int FollowersCount {
            get; internal set;
        }

        public int FolloweeCount {
            get; internal set;
        }
    }
}
