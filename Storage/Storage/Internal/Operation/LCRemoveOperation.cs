using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCRemoveOperation : ILCOperation {
        List<object> valueList;

        internal LCRemoveOperation(IEnumerable<object> values) {
            valueList = new List<object>(values);
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            if (previousOp is LCSetOperation || previousOp is LCDeleteOperation) {
                return previousOp;
            }
            if (previousOp is LCRemoveOperation removeOp) {
                List<object> list = new List<object>(removeOp.valueList);
                list.AddRange(valueList);
                valueList = list;
                return this;
            }
            throw new ArgumentException("Operation is invalid after previous operation.");
        }

        public object Encode() {
            return new Dictionary<string, object> {
                { "__op", "Remove" },
                { "objects", LCEncoder.Encode(valueList) }
            };
        }

        public object Apply(object oldValue, string key) {
            List<object> list = new List<object>();
            if (oldValue != null) {
                list.AddRange(oldValue as IEnumerable<object>);
            }
            list.RemoveAll(item => valueList.Contains(item));
            return list;
        }

        public IEnumerable GetNewObjectList() {
            return null;
        }
    }
}
