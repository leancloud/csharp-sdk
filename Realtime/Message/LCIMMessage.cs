using System;
using System.Collections.Generic;

namespace LeanCloud.Realtime {
    public abstract class LCIMMessage {
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

        public List<string> MentionList {
            get; set;
        }

        public bool MentionAll {
            get; set;
        }

        public LCIMMessage() {

        }

        internal abstract string Serialize();

        internal abstract string GetText();
        internal abstract byte[] GetBytes();
    }
}
