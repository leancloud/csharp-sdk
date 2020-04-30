using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 操作失败
    /// </summary>
    public class LCIMOperationFailure {
        /// <summary>
        /// 失败码
        /// </summary>
        public int Code {
            get; set;
        }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string Reason {
            get; set;
        }

        /// <summary>
        /// 失败数据
        /// </summary>
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
