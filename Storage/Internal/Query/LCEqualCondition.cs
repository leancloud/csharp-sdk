using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Query {
    internal class LCEqualCondition : ILCQueryCondition {
        readonly string key;
        readonly object value;

        internal LCEqualCondition(string key, object value) {
            this.key = key;
            this.value = value;
        }

        public bool Equals(ILCQueryCondition other) {
            if (other is LCEqualCondition cond) {
                return cond.key == key;
            }
            return false;
        }

        public Dictionary<string, object> Encode() {
            return new Dictionary<string, object> {
                { key, LCEncoder.Encode(value) }
            };
        }
    }
}
