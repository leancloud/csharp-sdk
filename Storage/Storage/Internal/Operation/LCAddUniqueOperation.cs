using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCAddUniqueOperation : ILCOperation {
        internal HashSet<object> values;

        internal LCAddUniqueOperation(IEnumerable<object> values) {
            this.values = new HashSet<object>(values);
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            if (previousOp is LCSetOperation || previousOp is LCDeleteOperation) {
                return previousOp;
            }
            if (previousOp is LCAddUniqueOperation addUniqueOp) {
                values.UnionWith(addUniqueOp.values);
                return this;
            }
            throw new ArgumentException("Operation is invalid after previous operation.");
        }

        public object Encode() {
            return new Dictionary<string, object> {
                { "__op", "AddUnique" },
                { "objects", LCEncoder.Encode(values.ToList()) }
            };
        }

        public object Apply(object oldValue, string key) {
            HashSet<object> set = new HashSet<object>();
            if (oldValue != null) {
                set.UnionWith(oldValue as IEnumerable<object>);
            }
            set.UnionWith(values);
            return set.ToList();
        }

        public IEnumerable GetNewObjectList() {
            return values;
        }
    }
}
