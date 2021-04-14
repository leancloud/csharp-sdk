using System.Collections.Generic;

namespace LeanCloud.Realtime {
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
