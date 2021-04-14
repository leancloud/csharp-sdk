using System;

namespace LeanCloud.Realtime {
    public class LCIMMessageQueryEndpoint {
        public string MessageId {
            get; set;
        }

        public long SentTimestamp {
            get; set;
        }

        public bool IsClosed {
            get; set;
        }

        public LCIMMessageQueryEndpoint() {

        }
    }

    public enum LCIMMessageQueryDirection {
        NewToOld,
        OldToNew
    }
}
