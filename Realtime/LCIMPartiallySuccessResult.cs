using System;
using System.Collections.Generic;
using LeanCloud.Storage;

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
    }
}
