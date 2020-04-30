namespace LeanCloud.Realtime {
    public class LCIMConversationMemberInfo {
        /// <summary>
        /// 群主
        /// </summary>
        public const string Owner = "Owner";

        /// <summary>
        /// 管理员
        /// </summary>
        public const string Manager = "Manager";

        /// <summary>
        /// 成员
        /// </summary>
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
