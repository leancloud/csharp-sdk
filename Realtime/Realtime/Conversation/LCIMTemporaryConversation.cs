using System;

namespace LeanCloud.Realtime {
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
