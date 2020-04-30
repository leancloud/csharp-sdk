using System;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 临时对话
    /// </summary>
    public class LCIMTemporaryConversation : LCIMConversation {
        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiredAt {
            get;
        }

        /// <summary>
        /// 是否过期
        /// </summary>
        public bool IsExpired {
            get {
                return DateTime.Now > ExpiredAt;
            }
        }

        public LCIMTemporaryConversation(LCIMClient client) : base(client) {
        }
    }
}
