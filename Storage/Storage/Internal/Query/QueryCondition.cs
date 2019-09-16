using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    internal class QueryOperationCondition : IQueryCondition {
        public string Key {
            get; set;
        }

        public string Op {
            get; set;
        }

        public object Value {
            get; set;
        }

        public bool Equals(IQueryCondition other) {
            if (other is QueryOperationCondition) {
                QueryOperationCondition otherCond = other as QueryOperationCondition;
                return Key == otherCond.Key && Op == otherCond.Op;
            }
            return false;
        }

        public IDictionary<string, object> ToJSON() {
            return new Dictionary<string, object> {
                { Key, new Dictionary<string, object> {
                    { Op, Value }
                } }
            };
        }
    }
}
