using System;
using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 消息基类
    /// </summary>
    public abstract class LCIMMessage {
        /// <summary>
        /// 消息所在对话 Id
        /// </summary>
        public string ConversationId {
            get; set;
        }

        /// <summary>
        /// 消息 Id
        /// </summary>
        public string Id {
            get; set;
        }

        /// <summary>
        /// 发送者 Id
        /// </summary>
        public string FromClientId {
            get; set;
        }

        /// <summary>
        /// 发送时间戳
        /// </summary>
        public long SentTimestamp {
            get; internal set;
        }

        /// <summary>
        /// 发送时间
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
        /// 送达时间
        /// </summary>
        public DateTime DeliveredAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(DeliveredTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// 已读时间戳
        /// </summary>
        public long ReadTimestamp {
            get; internal set;
        }

        /// <summary>
        /// 已读时间
        /// </summary>
        public DateTime ReadAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(ReadTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// 修改时间戳
        /// </summary>
        public long PatchedTimestamp {
            get; internal set;
        }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime PatchedAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(PatchedTimestamp)
                    .LocalDateTime;
            }
        }

        /// <summary>
        /// 提醒成员 Id 列表
        /// </summary>
        public List<string> MentionIdList {
            get; set;
        }

        /// <summary>
        /// 是否提醒所有人
        /// </summary>
        public bool MentionAll {
            get; set;
        }

        /// <summary>
        /// 是否提醒当前用户
        /// </summary>
        public bool Mentioned {
            get; internal set;
        }

        /// <summary>
        /// 是否是暂态消息
        /// </summary>
        public bool IsTransient {
            get; internal set;
        }

        internal LCIMMessage() {
        }
    }
}
