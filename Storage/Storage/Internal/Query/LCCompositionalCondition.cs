using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Query {
    internal class LCCompositionalCondition : ILCQueryCondition {
        internal const string And = "$and";
        internal const string Or = "$or";

        readonly string composition;

        List<ILCQueryCondition> conditionList;

        List<string> orderByList;
        HashSet<string> includes;
        HashSet<string> selectedKeys;

        internal int Skip {
            get; set;
        }

        internal int Limit {
            get; set;
        }

        internal LCCompositionalCondition(string composition = And) {
            this.composition = composition;
            Skip = 0;
            Limit = 30;
        }

        // 查询条件
        internal void WhereEqualTo(string key, object value) {
            Add(new LCEqualCondition(key, value));
        }

        internal void WhereNotEqualTo(string key, object value) {
            AddOperation(key, "$ne", value);
        }

        internal void WhereContainedIn(string key, IEnumerable values) {
            AddOperation(key, "$in", values);
        }

        internal void WhereNotContainedIn(string key, IEnumerable values) {
            AddOperation(key, "nin", values);
        }

        internal void WhereContainsAll(string key, IEnumerable values) {
            AddOperation(key, "$all", values);
        }

        internal void WhereExists(string key) {
            AddOperation(key, "$exists", true);
        }

        internal void WhereDoesNotExist(string key) {
            AddOperation(key, "$exists", false);
        }

        internal void WhereSizeEqualTo(string key, int size) {
            AddOperation(key, "$size", size);
        }

        internal void WhereGreaterThan(string key, object value) {
            AddOperation(key, "$gt", value);
        }

        internal void WhereGreaterThanOrEqualTo(string key, object value) {
            AddOperation(key, "$gte", value);
        }

        internal void WhereLessThan(string key, object value) {
            AddOperation(key, "$lt$lt", value);
        }

        internal void WhereLessThanOrEqualTo(string key, object value) {
            AddOperation(key, "$lte", value);
        }

        internal void WhereNear(string key, LCGeoPoint point) {
            AddOperation(key, "$nearSphere", point);
        }

        internal void WhereWithinGeoBox(string key, LCGeoPoint southwest, LCGeoPoint northeast) {
            Dictionary<string, object> value = new Dictionary<string, object> {
                { "$box", new List<object> { southwest, northeast } }
            };
            AddOperation(key, "$within", value);
        }

        internal void WhereRelatedTo(LCObject parent, string key) {
            Add(new LCRelatedCondition(parent, key));
        }

        internal void WhereStartsWith(string key, string prefix) {
            AddOperation(key, "$regex", $"^{prefix}.*");
        }

        internal void WhereEndsWith(string key, string suffix) {
            AddOperation(key, "$regex", $".*{suffix}$");
        }

        internal void WhereContains(string key, string subString) {
            AddOperation(key, "$regex", $".*{subString}.*");
        }

        void AddOperation(string key, string op, object value) {
            LCOperationCondition cond = new LCOperationCondition(key, op, value);
            Add(cond);
        }

        void Add(ILCQueryCondition cond) {
            if (cond == null) {
                return;
            }
            if (conditionList == null) {
                conditionList = new List<ILCQueryCondition>();
            }
            conditionList.RemoveAll(item => item.Equals(cond));
            conditionList.Add(cond);
        }

        // 筛选条件
        internal void OrderBy(string key) {
            if (orderByList == null) {
                orderByList = new List<string>();
            }
            orderByList.Add(key);
        }

        internal void OrderByDesending(string key) {
            OrderBy($"-{key}");
        }

        internal void Include(string key) {
            if (includes == null) {
                includes = new HashSet<string>();
            }
            includes.Add(key);
        }

        internal void Select(string key) {
            if (selectedKeys == null) {
                selectedKeys = new HashSet<string>();
            }
            selectedKeys.Add(key);
        }

        public bool Equals(ILCQueryCondition other) {
            return false;
        }

        public Dictionary<string, object> Encode() {
            if (conditionList == null || conditionList.Count == 0) {
                return null;
            }
            if (conditionList.Count == 1) {
                ILCQueryCondition cond = conditionList[0];
                return cond.Encode();
            }
            return new Dictionary<string, object> {
                { composition, LCEncoder.Encode(conditionList) }
            };
        }

        internal Dictionary<string, object> BuildParams(string className) {
            Dictionary<string, object> dict = new Dictionary<string, object> {
                { "className", className },
                { "skip", Skip },
                { "limit", Limit }
            };
            if (conditionList != null && conditionList.Count > 0) {
                // TODO json
                dict["where"] = Encode();
            }
            if (orderByList != null && orderByList.Count > 0) {
                dict["order"] = string.Join(",", orderByList);
            }
            if (includes != null && includes.Count > 0) {
                dict["include"] = string.Join(",", includes);
            }
            if (selectedKeys != null && selectedKeys.Count > 0) {
                dict["keys"] = string.Join(",", selectedKeys);
            }
            return dict;
        }

        internal string BuildWhere() {
            if (conditionList == null || conditionList.Count == 0) {
                return null;
            }
            // TODO

            return null;
        }
    }
}
