namespace LeanCloud.Storage {
    /// <summary>
    /// LCFriendshipRequest is a local representation of a friend request that
    /// is saved to LeanCloud.
    /// </summary>
    public class LCFriendshipRequest : LCObject {
        public const string CLASS_NAME = "_FriendshipRequest";

        public const int STATUS_PENDING = 0x01;
        public const int STATUS_ACCEPTED = 0x02;
        public const int STATUS_DECLINED = 0x04;
        public const int STATUS_ANY = 0x07;

        public LCUser User {
            get => base["user"] as LCUser;
        }

        public LCUser Friend {
            get => base["friend"] as LCUser;
        }

        public string Status {
            get => base["status"] as string;
        }

        public LCFriendshipRequest() : base(CLASS_NAME) {
        }

        public static LCQuery<LCFriendshipRequest> GetQuery() {
            return new LCQuery<LCFriendshipRequest>(CLASS_NAME);
        }
    }
}
