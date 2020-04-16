using System;
using System.Collections.Generic;

namespace LeanCloud.Realtime {
    public abstract class LCIMMessage {
        internal const int TextMessageType = -1;
        internal const int ImageMessageType = -2;
        internal const int AudioMessageType = -3;
        internal const int VideoMessageType = -4;
        internal const int LocationMessageType = -5;
        internal const int FileMessageType = -6;
            
        public string ConversationId {
            get; set;
        }

        public string Id {
            get; set;
        }

        public string FromClientId {
            get; set;
        }

        public long SentTimestamp {
            get; internal set;
        }

        public DateTime SentAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(SentTimestamp)
                    .LocalDateTime;
            }
        }

        public long DeliveredTimestamp {
            get; internal set;
        }

        public DateTime DeliveredAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(DeliveredTimestamp)
                    .LocalDateTime;
            }
        }

        public long ReadTimestamp {
            get; internal set;
        }

        public DateTime ReadAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(ReadTimestamp)
                    .LocalDateTime;
            }
        }

        public long PatchedTimestamp {
            get; internal set;
        }

        public DateTime PatchedAt {
            get {
                return DateTimeOffset.FromUnixTimeMilliseconds(PatchedTimestamp)
                    .LocalDateTime;
            }
        }

        public List<string> MentionIdList {
            get; set;
        }

        public bool MentionAll {
            get; set;
        }

        public bool Mentioned {
            get; internal set;
        }

        public bool IsTransient {
            get; internal set;
        }

        internal LCIMMessage() {
        }
    }
}
