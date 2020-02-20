using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCSetOperation : ILCOperation {
        object value;

        internal LCSetOperation(object value) {
            this.value = value;
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            return this;
        }

        public Dictionary<string, object> Encode() {
            return LCEncoder.Encode(value) as Dictionary<string, object>;
        }

        public object Apply(object oldValue, string key) {
            return value;
        }

        public IEnumerable GetNewObjectList() {
            if (value is IEnumerable enumerable) {
                return enumerable;
            }
            return new List<object> { value };
        }        
    }
}
