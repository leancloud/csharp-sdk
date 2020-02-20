using System;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Operation {
    internal class LCRemoveRelationOperation : ILCOperation {
        List<LCObject> valueList;

        internal LCRemoveRelationOperation(LCObject obj) {
            valueList = new List<LCObject> { obj };
        }

        public ILCOperation MergeWithPrevious(ILCOperation previousOp) {
            if (previousOp is LCSetOperation || previousOp is LCDeleteOperation) {
                return previousOp;
            }
            if (previousOp is LCRemoveRelationOperation removeRelationOp) {
                valueList.AddRange(removeRelationOp.valueList);
                return this;
            }
            throw new ArgumentException("Operation is invalid after previous operation.");
        }

        public Dictionary<string, object> Encode() {
            return new Dictionary<string, object> {
                { "__op", "RemoveRelation" },
                { "objects", LCEncoder.Encode(valueList) }
            };
        }

        public object Apply(object oldValue, string key) {
            LCRelation<LCObject> relation = new LCRelation<LCObject>();
            relation.targetClass = valueList[0].ClassName;
            return relation;
        }

        public IEnumerable GetNewObjectList() {
            return null;
        }
    }
}
