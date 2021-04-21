using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMPartiallySuccessResult is the partially successful result of a conversation operation.
    /// </summary>
    public class LCIMPartiallySuccessResult {
        public List<string> SuccessfulClientIdList {
            get; internal set;
        }

        public List<LCIMOperationFailure> FailureList {
            get; internal set;
        }

        public LCIMPartiallySuccessResult() {
        }

        public bool IsSuccess => FailureList == null || FailureList.Count == 0;
    }
}
