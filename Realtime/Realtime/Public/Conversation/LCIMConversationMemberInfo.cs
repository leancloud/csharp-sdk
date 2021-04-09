namespace LeanCloud.Realtime {
    public class LCIMConversationMemberInfo {
        public const string Owner = "Owner";

        public const string Manager = "Manager";

        public const string Member = "Member";

        public string ConversationId {
            get; set;
        }

        public string MemberId {
            get; set;
        }

        public string Role {
            get; set;
        }

        public bool IsOwner {
            get {
                return Role == Owner;
            }
        }

        public bool IsManager {
            get {
                return Role == Manager;
            }
        }
    }
}
