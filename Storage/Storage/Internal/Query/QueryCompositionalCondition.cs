using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LeanCloud.Storage.Internal {
    public class QueryCombinedCondition : IQueryCondition {
        public const string AND = "$and";
        public const string OR = "$or";

        readonly List<IQueryCondition> conditions;
        readonly string composition;

        internal List<string> orderBy;
        internal HashSet<string> includes;
        internal HashSet<string> selectedKeys;
        internal int skip;
        internal int limit;

        public QueryCombinedCondition(string composition = AND) {
            conditions = new List<IQueryCondition>();
            this.composition = composition;
            skip = 0;
            limit = 30;
        }

        #region IQueryCondition

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

        #endregion

        #region where

        public void WhereContainedIn<T>(string key, IEnumerable<T> values) {
            AddCondition(key, "$in", values.ToList());
        }

        public void WhereContainsAll<T>(string key, IEnumerable<T> values) {
            AddCondition(key, "$all", values.ToList());
        }

        public void WhereContains(string key, string substring) {
            AddCondition(key, "$regex", RegexQuote(substring));
        }

        public void WhereDoesNotExist(string key) {
            AddCondition(key, "$exists", false);
        }

        public void WhereDoesNotMatchQuery<T>(string key, AVQuery<T> query) where T : AVObject {
            AddCondition(key, "$notInQuery", query.BuildParameters(query.ClassName));
        }

        public void WhereEndsWith(string key, string suffix) {
            AddCondition(key, "$regex", RegexQuote(suffix) + "$");
        }

        public void WhereEqualTo(string key, object value) {
            AddCondition(new QueryEqualCondition(key, value));
        }

        public void WhereSizeEqualTo(string key, uint size) {
            AddCondition(key, "$size", size);
        }

        public void WhereExists(string key) {
            AddCondition(key, "$exists", true);
        }

        public void WhereGreaterThan(string key, object value) {
            AddCondition(key, "$gt", value);
        }

        public void WhereGreaterThanOrEqualTo(string key, object value) {
            AddCondition(key, "$gte", value);
        }

        public void WhereLessThan(string key, object value) {
            AddCondition(key, "$lt", value);
        }

        public void WhereLessThanOrEqualTo(string key, object value) {
            AddCondition(key, "$lte", value);
        }

        public void WhereMatches(string key, Regex regex, string modifiers) {
            if (!regex.Options.HasFlag(RegexOptions.ECMAScript)) {
                throw new ArgumentException(
                  "Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.");
            }
            AddCondition(key, "$regex", regex.ToString());
            AddCondition(key, "options", modifiers);
        }

        public void WhereMatches(string key, Regex regex) {
            WhereMatches(key, regex, null);
        }

        public void WhereMatches(string key, string pattern, string modifiers) {
            WhereMatches(key, new Regex(pattern, RegexOptions.ECMAScript), modifiers);
        }

        public void WhereMatches(string key, string pattern) {
            WhereMatches(key, pattern, null);
        }

        public void WhereMatchesKeyInQuery<T>(string key, string keyInQuery, AVQuery<T> query) where T : AVObject {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(query.ClassName)},
                { "key", keyInQuery}
            };
            AddCondition(key, "$select", parameters);
        }

        public void WhereDoesNotMatchesKeyInQuery<T>(string key, string keyInQuery, AVQuery<T> query) where T : AVObject {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(query.ClassName)},
                { "key", keyInQuery}
            };
            AddCondition(key, "$dontSelect", parameters);
        }

        public void WhereMatchesQuery<T>(string key, AVQuery<T> query) where T : AVObject {
            AddCondition(key, "$inQuery", query.BuildParameters(query.ClassName));
        }

        public void WhereNear(string key, AVGeoPoint point) {
            AddCondition(key, "$nearSphere", point);
        }

        public void WhereNotContainedIn<T>(string key, IEnumerable<T> values) {
            AddCondition(key, "$nin", values.ToList());
        }

        public void WhereNotEqualTo(string key, object value) {
            AddCondition(key, "$ne", value);
        }

        public void WhereStartsWith(string key, string suffix) {
            AddCondition(key, "$regex", "^" + RegexQuote(suffix));
        }

        public void WhereWithinGeoBox(string key, AVGeoPoint southwest, AVGeoPoint northeast) {
            Dictionary<string, object> value = new Dictionary<string, object> {
                { "$box", new[] { southwest, northeast } }
            };
            AddCondition(key, "$within", value);
        }

        public void WhereWithinDistance(string key, AVGeoPoint point, AVGeoDistance maxDistance) {
            AddCondition(key, "$nearSphere", point);
            AddCondition(key, "$maxDistance", maxDistance.Radians);
        }

        public void WhereRelatedTo(AVObject parent, string key) {
            AddCondition(new QueryRelatedCondition(parent, key));
        }

        #endregion

        public void OrderBy(string key) {
            if (orderBy == null) {
                orderBy = new List<string>();
            }
            orderBy.Add(key);
        }

        public void OrderByDescending(string key) {
            if (orderBy == null) {
                orderBy = new List<string>();
            }
            orderBy.Add($"-{key}");
        }

        public void Include(string key) {
            if (includes == null) {
                includes = new HashSet<string>();
            }
            try {
                includes.Add(key);
            } catch (Exception e) {
                AVClient.PrintLog(e.Message);
            }
        }

        public void Select(string key) {
            if (selectedKeys == null) {
                selectedKeys = new HashSet<string>();
            }
            try {
                selectedKeys.Add(key);
            } catch (Exception e) {
                AVClient.PrintLog(e.Message);
            }
        }

        public void Skip(int count) {
            skip = count;
        }

        public void Limit(int count) {
            limit = count;
        }

        public void AddCondition(string key, string op, object value) {
            QueryOperationCondition cond = new QueryOperationCondition {
                Key = key,
                Op = op,
                Value = value
            };
            AddCondition(cond);
        }

        public void AddCondition(IQueryCondition condition) {
            if (condition == null) {
                return;
            }
            // 组合查询的 Key 为 null
            conditions.RemoveAll(cond => {
                return cond.Equals(condition);
            });
            conditions.Add(condition);
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
            result["skip"] = skip;
            result["limit"] = limit;
            return result;
        }

        string RegexQuote(string input) {
            return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
        }
    }
}
