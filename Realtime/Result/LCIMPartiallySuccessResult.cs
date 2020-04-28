using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 部分成功结果
    /// </summary>
    public class LCIMPartiallySuccessResult {
        /// <summary>
        /// 成功数据集
        /// </summary>
        public List<string> SuccessfulClientIdList {
            get; internal set;
        }

        /// <summary>
        /// 失败原因
        /// </summary>
        public List<LCIMOperationFailure> FailureList {
            get; internal set;
        }

        public LCIMPartiallySuccessResult() {
        }

        public bool IsSuccess => FailureList == null || FailureList.Count == 0;
    }
}
