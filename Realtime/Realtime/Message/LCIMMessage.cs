using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    /// <summary>
    /// The base class of message.
    /// </summary>
    public abstract class LCIMMessage {
        /// <summary>
        /// The conversation ID this message belongs to.
        /// </summary>
        public string ConversationId {
            get; set;
        }

        /// <summary>
        /// The ID of this message.
        /// </summary>
        public string Id {
            get; set;
        }

        /// <summary>
        /// The ID of the client who sends this message.
        /// </summary>
        public string FromClientId {
            get; set;
        }

        /// <summary>
        /// The timestamp of this message.
        /// </summary>
        public long SentTimestamp {
            get; internal set;
        }

        /// <summary>
        /// The sending date of this message.
        /// </summary>
        public DateTime SentAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(SentTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// 送达时间戳
        /// </summary>
        public long DeliveredTimestamp {
            get; internal set;
        }

        /// <summary>
        /// The delivered date of this message.
        /// </summary>
        public DateTime DeliveredAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(DeliveredTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// The timestamp when this message has been read by others.
        /// </summary>
        public long ReadTimestamp {
            get; internal set;
        }

        /// <summary>
        /// When this message has been read by others.
        /// </summary>
        public DateTime ReadAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(ReadTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// The timestamp when this message is updated.
        /// </summary>
        public long PatchedTimestamp {
            get; internal set;
        }

        /// <summary>
        /// When this message is updated. 
        /// </summary>
        public DateTime PatchedAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(PatchedTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// The members in the conversation mentioned by this message. 
        /// </summary>
        public List<string> MentionIdList {
            get; set;
        }

        /// <summary>
        /// Whether all members in the conversation are mentioned by this message.
        /// </summary>
        public bool MentionAll {
            get; set;
        }

        /// <summary>
        /// Whether the current user has been mentioned in this message.
        /// </summary>
        public bool Mentioned {
            get; internal set;
        }

        /// <summary>
        /// Indicates whether this message is transient.
        /// </summary>
        public bool IsTransient {
            get; internal set;
        }

        internal LCIMMessage() {
        }

        internal virtual Task PrepareSend() {
            return Task.CompletedTask;
        }
    }
}
