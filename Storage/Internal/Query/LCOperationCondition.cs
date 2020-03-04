using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Query {
    internal class LCOperationCondition : ILCQueryCondition {
        readonly string key;
        readonly string op;
        readonly object value;

        internal LCOperationCondition(string key, string op, object value) {
            this.key = key;
            this.op = op;
            this.value = value;
        }

        public bool Equals(ILCQueryCondition other) {
            if (other is LCOperationCondition cond) {
                return cond.key == key && cond.op == op;
            }
            return false;
        }

        public Dictionary<string, object> Encode() {
            return new Dictionary<string, object> {
                { key, new Dictionary<string, object> {
                    { op, LCEncoder.Encode(value) }
                } }
            };
        }
    }
}
