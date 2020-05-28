using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage.Internal.Query {
    public class LCCompositionalCondition : ILCQueryCondition {
        public const string And = "$and";
        public const string Or = "$or";

        readonly string composition;

        List<ILCQueryCondition> conditionList;

        List<string> orderByList;
        HashSet<string> includes;
        HashSet<string> selectedKeys;

        public int Skip {
            get; set;
        }

        public int Limit {
            get; set;
        }

        public bool IncludeACL {
            get; set;
        }

        public LCCompositionalCondition(string composition = And) {
            this.composition = composition;
            Skip = 0;
            Limit = 30;
        }

        // 查询条件
        public void WhereEqualTo(string key, object value) {
            Add(new LCEqualCondition(key, value));
        }

        public void WhereNotEqualTo(string key, object value) {
            AddOperation(key, "$ne", value);
        }

        public void WhereContainedIn(string key, IEnumerable values) {
            AddOperation(key, "$in", values);
        }

        public void WhereNotContainedIn(string key, IEnumerable values) {
            AddOperation(key, "$nin", values);
        }

        public void WhereContainsAll(string key, IEnumerable values) {
            AddOperation(key, "$all", values);
        }

        public void WhereExists(string key) {
            AddOperation(key, "$exists", true);
        }

        public void WhereDoesNotExist(string key) {
            AddOperation(key, "$exists", false);
        }

        public void WhereSizeEqualTo(string key, int size) {
            AddOperation(key, "$size", size);
        }

        public void WhereGreaterThan(string key, object value) {
            AddOperation(key, "$gt", value);
        }

        public void WhereGreaterThanOrEqualTo(string key, object value) {
            AddOperation(key, "$gte", value);
        }

        public void WhereLessThan(string key, object value) {
            AddOperation(key, "$lt", value);
        }

        public void WhereLessThanOrEqualTo(string key, object value) {
            AddOperation(key, "$lte", value);
        }

        public void WhereNear(string key, LCGeoPoint point) {
            AddOperation(key, "$nearSphere", point);
        }

        public void WhereWithinGeoBox(string key, LCGeoPoint southwest, LCGeoPoint northeast) {
            Dictionary<string, object> value = new Dictionary<string, object> {
                { "$box", new List<object> { southwest, northeast } }
            };
            AddOperation(key, "$within", value);
        }

        public void WhereRelatedTo(LCObject parent, string key) {
            Add(new LCRelatedCondition(parent, key));
        }

        public void WhereStartsWith(string key, string prefix) {
            AddOperation(key, "$regex", $"^{prefix}.*");
        }

        public void WhereEndsWith(string key, string suffix) {
            AddOperation(key, "$regex", $".*{suffix}$");
        }

        public void WhereContains(string key, string subString) {
            AddOperation(key, "$regex", $".*{subString}.*");
        }

        public void WhereMatches(string key, string regex, string modifiers) {
            Dictionary<string, object> value = new Dictionary<string, object> {
                { "$regex", regex }
            };
            if (modifiers != null) {
                value["$options"] = modifiers;
            }
            Add(new LCEqualCondition(key, value));
        }

        public void WhereMatchesQuery<K>(string key, LCQuery<K> query) where K : LCObject {
            Dictionary<string, object> inQuery = new Dictionary<string, object> {
                { "where", query.Condition },
                { "className", query.ClassName }
            };
            AddOperation(key, "$inQuery", inQuery);
        }

        public void WhereDoesNotMatchQuery<K>(string key, LCQuery<K> query) where K : LCObject {
            Dictionary<string, object> inQuery = new Dictionary<string, object> {
                { "where", query.Condition },
                { "className", query.ClassName }
            };
            AddOperation(key, "$notInQuery", inQuery);
        }

        void AddOperation(string key, string op, object value) {
            LCOperationCondition cond = new LCOperationCondition(key, op, value);
            Add(cond);
        }

        public void Add(ILCQueryCondition cond) {
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
        public void OrderByAscending(string key) {
            orderByList = new List<string>();
            orderByList.Add(key);
        }

        public void OrderByDescending(string key) {
            OrderByAscending($"-{key}");
        }

        public void AddAscendingOrder(string key) {
            if (orderByList == null) {
                orderByList = new List<string>();
            }
            orderByList.Add(key);
        }

        public void AddDescendingOrder(string key) {
            AddAscendingOrder($"-{key}");
        }

        public void Include(string key) {
            if (includes == null) {
                includes = new HashSet<string>();
            }
            includes.Add(key);
        }

        public void Select(string key) {
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

        public Dictionary<string, object> BuildParams() {
            Dictionary<string, object> dict = new Dictionary<string, object> {
                { "skip", Skip },
                { "limit", Limit }
            };
            if (conditionList != null && conditionList.Count > 0) {
                dict["where"] = JsonConvert.SerializeObject(Encode());
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
            if (IncludeACL) {
                dict["returnACL"] = "true";
            }
            return dict;
        }

        public string BuildWhere() {
            if (conditionList == null || conditionList.Count == 0) {
                return null;
            }
            return JsonConvert.SerializeObject(Encode()); 
        }
    }
}
