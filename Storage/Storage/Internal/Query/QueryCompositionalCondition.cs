using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LeanCloud.Storage.Internal {
    internal class QueryCompositionalCondition : IQueryCondition {
        internal const string AND = "$and";
        internal const string OR = "$or";

        readonly List<IQueryCondition> conditions;
        readonly string composition;

        internal ReadOnlyCollection<string> orderBy;
        internal HashSet<string> includes;
        internal HashSet<string> selectedKeys;
        internal string redirectClassNameForKey;
        internal int skip;
        internal int limit;

        internal QueryCompositionalCondition(string composition = AND) {
            conditions = new List<IQueryCondition>();
            this.composition = composition;
            skip = 0;
            limit = 30;
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

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object> BuildParameters(string className) {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (conditions != null) {
                result["where"] = ToJSON();
            }
            if (orderBy != null) {
                result["order"] = string.Join(",", orderBy.ToArray());
            }
            if (includes != null) {
                result["include"] = string.Join(",", includes.ToArray());
            }
            if (selectedKeys != null) {
                result["keys"] = string.Join(",", selectedKeys.ToArray());
            }
            if (!string.IsNullOrEmpty(className)) {
                result["className"] = className;
            }
            if (redirectClassNameForKey != null) {
                result["redirectClassNameForKey"] = redirectClassNameForKey;
            }
            result["skip"] = skip;
            result["limit"] = limit;
            return result;
        }

        internal void OrderBy(string key) {
            orderBy = new ReadOnlyCollection<string>(new List<string> { key });
        }

        internal void OrderByDescending(string key) {
            orderBy = new ReadOnlyCollection<string>(new List<string> { "-" + key });
        }

        internal void Include(string key) {
            if (includes == null) {
                includes = new HashSet<string>();
            }
            try {
                includes.Add(key);
            } catch (Exception e) {
                AVClient.PrintLog(e.Message);
            }
        }

        internal void Select(string key) {
            if (selectedKeys == null) {
                selectedKeys = new HashSet<string>();
            }
            try {
                selectedKeys.Add(key);
            } catch (Exception e) {
                AVClient.PrintLog(e.Message);
            }
        }

        internal void Skip(int count) {
            skip = count;
        }

        internal void Limit(int count) {
            limit = count;
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
