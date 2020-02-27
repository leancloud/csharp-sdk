using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCAddRelationOperation : ILCOperation {
        List<LCObject> valueList;

        internal LCAddRelationOperation(IEnumerable<LCObject> objects) {
            valueList = new List<LCObject>(objects);
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            if (previousOp is LCSetOperation || previousOp is LCDeleteOperation) {
                return previousOp;
            }
            if (previousOp is LCAddRelationOperation addRelationOp) {
                valueList.AddRange(addRelationOp.valueList);
                return this;
            }
            throw new ArgumentException("Operation is invalid after previous operation.");
        }

        public object Encode() {
            return new Dictionary<string, object> {
                { "__op", "AddRelation" },
                { "objects", LCEncoder.Encode(valueList) }
            };
        }

        public object Apply(object oldValue, string key) {
            LCRelation<LCObject> relation = new LCRelation<LCObject>();
            relation.TargetClass = valueList[0].ClassName;
            return relation;
        }

        public IEnumerable GetNewObjectList() {
            return valueList;
        }
    }
}
