using System;

namespace LeanCloud.Realtime {
    public class LCIMConversationMemberInfo {
        public string ConversationId {
            get; set;
        }

        public string MemberId {
            get; set;
        }

        public bool IsOwner {
            get; set;
        }

        public string Role {
            get; set;
        }
    }
}
