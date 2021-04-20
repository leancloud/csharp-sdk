using System.Collections.Generic;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMPartiallySuccessResult is the result that handles the operation of conversation.
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
