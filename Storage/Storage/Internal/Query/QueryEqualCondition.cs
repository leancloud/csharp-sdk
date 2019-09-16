using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    public class QueryEqualCondition : IQueryCondition {
        string key;
        object value;

        public QueryEqualCondition(string key, object value) {
            this.key = key;
            this.value = value;
        }

        public bool Equals(IQueryCondition other) {
            if (other is QueryEqualCondition) {
                QueryEqualCondition otherCond = other as QueryEqualCondition;
                return key == otherCond.key;
            }
            return false;
        }

        public IDictionary<string, object> ToJSON() {
            return new Dictionary<string, object> {
                { key, PointerOrLocalIdEncoder.Instance.Encode(value) }
            };
        }
    }
}
