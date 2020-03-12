using System;
using System.Collections.Generic;

namespace LeanCloud.Realtime {
    public class LCIMMessage {
        public string ConversationId {
            get; set;
        }

        public string Id {
            get; set;
        }

        public string FromClientId {
            get; set;
        }

        public int SentTimestamp {
            get; set;
        }

        public DateTime SentAt {
            get; set;
        }

        public int DeliveredTimestamp {
            get; set;
        }

        public DateTime DeliveredAt {
            get; set;
        }

        public int ReadTimestamp {
            get; set;
        }

        public DateTime ReadAt {
            get; set;
        }

        public int PatchedTimestamp {
            get; set;
        }

        public DateTime PatchedAt {
            get; set;
        }

        public List<string> MentionList {
            get; set;
        }

        public LCIMMessage() {

        }

        
    }
}
