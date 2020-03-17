using System.Linq;
using System.Collections.Generic;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime {
    public class LCIMOperationFailure {
        public int Code {
            get; set;
        }

        public string Reason {
            get; set;
        }

        public List<string> MemberList {
            get; set;
        }

        public LCIMOperationFailure(ErrorCommand error) {
            Code = error.Code;
            Reason = error.Reason;
            MemberList = error.Pids.ToList();
        }
    }
}
