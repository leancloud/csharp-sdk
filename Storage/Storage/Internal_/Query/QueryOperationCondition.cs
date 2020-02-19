using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    internal class QueryOperationCondition : IQueryCondition {
        readonly string key;
        readonly string op;
        readonly object value;

        public QueryOperationCondition(string key, string op, object value) {
            this.key = key;
            this.op = op;
            this.value = value;
        }

        public bool Equals(IQueryCondition other) {
            if (other is QueryOperationCondition otherCond) {
                return key == otherCond.key && op == otherCond.op;
            }
            return false;
        }

        public IDictionary<string, object> ToJSON() {
            return new Dictionary<string, object> {
                { key, new Dictionary<string, object> {
                    { op, value }
                } }
            };
        }
    }
}
