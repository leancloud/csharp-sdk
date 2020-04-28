namespace LeanCloud.Realtime {
    /// <summary>
    /// 消息优先级
    /// </summary>
    public enum LCIMMessagePriority {
        Hight = 1,
        Normal = 2,
        Low = 3
    }

    /// <summary>
    /// 发送消息选项
    /// </summary>
    public class LCIMMessageSendOptions {
        /// <summary>
        /// 是否作为暂态消息发送
        /// </summary>
        public bool Transient {
            get; set;
        }

        /// <summary>
        /// 是否需要消息回执，仅在普通对话中有效
        /// </summary>
        public bool Receipt {
            get; set;
        }

        /// <summary>
        /// 是否作为遗愿消息发送
        /// </summary>
        public bool Will {
            get; set;
        }

        /// <summary>
        /// 消息优先级，仅在暂态对话中有效
        /// </summary>
        public LCIMMessagePriority Priority {
            get; set;
        }

        public static LCIMMessageSendOptions Default = new LCIMMessageSendOptions();
    }
}
