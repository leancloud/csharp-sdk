using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMOperationFailure is the result that handles the operation of conversation.
    /// </summary>
    public class LCIMOperationFailure {
        public int Code {
            get; set;
        }

        public string Reason {
            get; set;
        }

        public List<string> IdList {
            get; set;
        }

        //public LCIMOperationFailure(ErrorCommand error) {
        //    Code = error.Code;
        //    Reason = error.Reason;
        //    MemberList = error.Pids.ToList();
        //}
    }
}
