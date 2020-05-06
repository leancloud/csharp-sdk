using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCAddOperation : ILCOperation {
        internal List<object> valueList;

        internal LCAddOperation(IEnumerable<object> values) {
            valueList = new List<object>(values);
        }

        ILCOperation ILCOperation.MergeWithPrevious(ILCOperation previousOp) {
            if (previousOp is LCSetOperation || previousOp is LCDeleteOperation) {
                return previousOp;
            }
            if (previousOp is LCAddOperation addOp) {
                List<object> list = new List<object>(addOp.valueList);
                list.AddRange(valueList);
                valueList = list;
                return this;
            }
            if (previousOp is LCAddUniqueOperation addUniqueOp) {
                List<object> list = addUniqueOp.values.ToList();
                list.AddRange(valueList);
                valueList = list;
                return this;
            }
            throw new ArgumentException("Operation is invalid after previous operation.");
        }

        object ILCOperation.Encode() {
            return new Dictionary<string, object> {
                { "__op", "Add" },
                { "objects", LCEncoder.Encode(valueList) }
            };
        }

        object ILCOperation.Apply(object oldValue, string key) {
            List<object> list = new List<object>();
            if (oldValue != null) {
                list.AddRange(oldValue as IEnumerable<object>);
            }
            list.AddRange(valueList);
            return list;
        }

        IEnumerable ILCOperation.GetNewObjectList() {
            return valueList;
        }
    }
}
