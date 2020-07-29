using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// The priority for sending messages in chatroom.
    /// </summary>
    public enum LCIMMessagePriority {
        Hight = 1,
        Normal = 2,
        Low = 3
    }

    /// <summary>
    /// The options for sending message.
    /// </summary>
    public class LCIMMessageSendOptions {
        /// <summary>
        /// Whether this is a transient message.
        /// </summary>
        public bool Transient {
            get; set;
        }

        /// <summary>
        /// Whether receipts are needed, only for normal conversations.
        /// </summary>
        public bool Receipt {
            get; set;
        }

        /// <summary>
        /// Whether this is a will message,
        /// which will be sent automatically when a user goes offline unexpectedly.
        /// </summary>
        public bool Will {
            get; set;
        }

        /// <summary>
        /// The priority for sending messages in chatroom. 
        /// </summary>
        public LCIMMessagePriority Priority {
            get; set;
        }

        public Dictionary<string, object> PushData {
            get; set;
        }

        public static LCIMMessageSendOptions Default = new LCIMMessageSendOptions();
    }
}
