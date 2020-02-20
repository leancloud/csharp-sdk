using System;
using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCDecrementOperation : ILCOperation {
        

        internal LCDecrementOperation() {
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> Encode() {
            throw new NotImplementedException();
        }

        public object Apply(object oldValue, string key) {
            throw new NotImplementedException();
        }

        public IEnumerable GetNewObjectList() {
            throw new NotImplementedException();
        }        
    }
}
