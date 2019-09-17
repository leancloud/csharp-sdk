using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    internal class QueryRelatedCondition : IQueryCondition {
        AVObject parent;
        string key;

        public QueryRelatedCondition(AVObject parent, string key) {
            this.parent = parent;
            this.key = key;
        }

        public bool Equals(IQueryCondition other) {
            if (other is QueryRelatedCondition otherCond) {
                return key == otherCond.key;
            }
            return false;
        }

        public IDictionary<string, object> ToJSON() {
            return new Dictionary<string, object> {
                { "$relatedTo", new Dictionary<string, object> {
                    { "object", PointerOrLocalIdEncoder.Instance.Encode(parent) },
                    { "key", key }
                } }
            };
        }
    }
}
