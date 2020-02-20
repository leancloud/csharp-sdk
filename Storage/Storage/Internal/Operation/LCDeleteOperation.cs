using System;
using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCDeleteOperation : ILCOperation {
        internal LCDeleteOperation() {
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            return this;
        }

        public Dictionary<string, object> Encode() {
            return new Dictionary<string, object> {
                { "__op", "Delete" }
            };
        }

        public object Apply(object oldValue, string key) {
            return null;
        }

        public IEnumerable GetNewObjectList() {
            return null;
        }
    }
}
