using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    internal class QueryCompositionalCondition : IQueryCondition {
        internal const string AND = "$and";
        internal const string OR = "$or";

        readonly List<IQueryCondition> conditions;
        readonly string composition;

        internal QueryCompositionalCondition(string composition = AND) {
            conditions = new List<IQueryCondition>();
            this.composition = composition;
        }

        public bool Equals(IQueryCondition other) {
            return false;
        }

        public IDictionary<string, object> ToJSON() {
            if (conditions == null || conditions.Count == 0) {
                return null;
            }
            if (conditions.Count == 1) {
                return conditions[0].ToJSON();
            }
            List<object> list = new List<object>();
            foreach (IQueryCondition cond in conditions) {
                list.Add(cond.ToJSON());
            }
            return new Dictionary<string, object> {
                { composition, list }
            };
        }

        internal void AddCondition(IQueryCondition condition) {
            if (condition == null) {
                return;
            }
            // 组合查询的 Key 为 null
            conditions.RemoveAll(cond => {
                return cond.Equals(condition);
            });
            conditions.Add(condition);
        }
    }
}
