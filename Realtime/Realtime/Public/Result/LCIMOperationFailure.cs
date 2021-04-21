using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMOperationFailure is the failed result of a conversation operation.
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
