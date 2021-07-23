namespace LeanCloud.Storage {
    /// <summary>
    /// LCFriendshipRequest is a local representation of a friend request that
    /// is saved to LeanCloud.
    /// </summary>
    public class LCFriendshipRequest : LCObject {
        public const string CLASS_NAME = "_FriendshipRequest";

        public LCFriendshipRequest() : base(CLASS_NAME) {
        }
    }
}
