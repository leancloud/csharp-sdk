using System;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMTemporaryConversation is a local representation of temporary conversation
    /// in LeanCloud.
    /// </summary>
    public class LCIMTemporaryConversation : LCIMConversation {
        public DateTime ExpiredAt {
            get;
        }

        public bool IsExpired {
            get {
                return DateTime.Now > ExpiredAt;
            }
        }

        public LCIMTemporaryConversation(LCIMClient client) : base(client) {
        }
    }
}
