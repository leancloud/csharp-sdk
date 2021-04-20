using System;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMMessageQueryEndpoint is the parameter that controls the limitation
    /// of querying messages.
    /// </summary>
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
