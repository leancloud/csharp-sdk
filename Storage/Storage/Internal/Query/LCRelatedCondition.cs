using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Query {
    public class LCRelatedCondition : ILCQueryCondition {
        readonly LCObject parent;
        readonly string key;

        public LCRelatedCondition(LCObject parent, string key) {
            this.parent = parent;
            this.key = key;
        }

        public bool Equals(ILCQueryCondition other) {
            if (other is LCRelatedCondition cond) {
                return cond.key == key;
            }
            return false;
        }

        public Dictionary<string, object> Encode() {
            return new Dictionary<string, object> {
                { "$relatedTo", new Dictionary<string, object> {
                    { "object", LCEncoder.Encode(parent) },
                    { "key", key }
                } }
            };
        }
    }
}
