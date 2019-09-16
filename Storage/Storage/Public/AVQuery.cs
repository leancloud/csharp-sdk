using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    public class AVQuery<T> where T : AVObject {
        public string ClassName {
            get; internal set;
        }

        private string path;
        public string Path {
            get {
                if (string.IsNullOrEmpty(path)) {
                    return $"classes/{Uri.EscapeDataString(ClassName)}";
                }
                return path;
            }
            set {
                path = value;
            }
        }

        internal QueryCompositionalCondition condition;

        internal ReadOnlyCollection<string> orderBy;
        internal ReadOnlyCollection<string> includes;
        internal ReadOnlyCollection<string> selectedKeys;
        internal string redirectClassNameForKey;
        internal int? skip;
        internal int? limit;

        internal static AVQueryController QueryController {
            get {
                return AVPlugins.Instance.QueryController;
            }
        }

        internal static ObjectSubclassingController SubclassingController {
            get {
                return AVPlugins.Instance.SubclassingController;
            }
        }

        public AVQuery()
            : this(SubclassingController.GetClassName(typeof(T))) {
        }

        public AVQuery(string className) {
            if (string.IsNullOrEmpty(className)) {
                throw new ArgumentNullException(nameof(className));
            }
            ClassName = className;
            condition = new QueryCompositionalCondition();
        }

        public static AVQuery<T> And(IEnumerable<AVQuery<T>> queries) {
            AVQuery<T> composition = new AVQuery<T>();
            string className = null;
            if (queries != null) {
                foreach (AVQuery<T> query in queries) {
                    if (className != null && className != query.ClassName) {
                        throw new ArgumentException("All of the queries in an or query must be on the same class.");
                    }
                    composition.AddCondition(query.condition);
                    className = query.ClassName;
                }
            }
            composition.ClassName = className;
            return composition;
        }

        public static AVQuery<T> Or(IEnumerable<AVQuery<T>> queries) {
            AVQuery<T> composition = new AVQuery<T> {
                condition = new QueryCompositionalCondition(QueryCompositionalCondition.OR)
            };
            string className = null;
            if (queries != null) {
                foreach (AVQuery<T> query in queries) {
                    if (className != null && className != query.ClassName) {
                        throw new ArgumentException("All of the queries in an or query must be on the same class.");
                    }
                    composition.AddCondition(query.condition);
                    className = query.ClassName;
                }
            }
            composition.ClassName = className;
            return composition;
        }

        public virtual async Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken = default) {
            IEnumerable<IObjectState> states = await QueryController.FindAsync(this, AVUser.CurrentUser, cancellationToken);
            return (from state in states
                    select AVObject.FromState<T>(state, ClassName));
        }

        public virtual async Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default) {
            IObjectState state = await QueryController.FirstAsync<T>(this, AVUser.CurrentUser, cancellationToken);
            return state == null ? default : AVObject.FromState<T>(state, ClassName);
        }

        public virtual async Task<T> FirstAsync(CancellationToken cancellationToken = default) {
            var result = await FirstOrDefaultAsync(cancellationToken);
            if (result == null) {
                throw new AVException(AVException.ErrorCode.ObjectNotFound,
                  "No results matched the query.");
            }
            return result;
        }

        public virtual Task<int> CountAsync(CancellationToken cancellationToken = default) {
            return QueryController.CountAsync(this, AVUser.CurrentUser, cancellationToken);
        }

        public virtual async Task<T> GetAsync(string objectId, CancellationToken cancellationToken) {
            AVQuery<T> singleItemQuery = new AVQuery<T>(ClassName)
                .WhereEqualTo("objectId", objectId);
            singleItemQuery.includes = includes;
            singleItemQuery.selectedKeys = selectedKeys;
            singleItemQuery.limit = 1;
            var result = await singleItemQuery.FindAsync(cancellationToken);
            var first = result.FirstOrDefault();
            if (first == null) {
                throw new AVException(AVException.ErrorCode.ObjectNotFound,
                  "Object with the given objectId not found.");
            }
            return first;
        }

        #region CQL
        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql">CQL 语句</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>返回符合条件的对象集合</returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql, CancellationToken cancellationToken = default) {
            var queryString = $"cloudQuery?cql={Uri.EscapeDataString(cql)}";
            return RebuildObjectFromCloudQueryResult(queryString);
        }

        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cqlTeamplate">带有占位符的模板 cql 语句</param>
        /// <param name="pvalues">占位符对应的参数数组</param>
        /// <returns></returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cqlTeamplate, params object[] pvalues) {
            string queryStringTemplate = "cloudQuery?cql={0}&pvalues={1}";
            string pSrting = JsonConvert.SerializeObject(pvalues);
            string queryString = string.Format(queryStringTemplate, Uri.EscapeDataString(cqlTeamplate), Uri.EscapeDataString(pSrting));

            return RebuildObjectFromCloudQueryResult(queryString);
        }

        internal static async Task<IEnumerable<T>> RebuildObjectFromCloudQueryResult(string queryString) {
            var command = new AVCommand {
                Path = queryString,
                Method = HttpMethod.Get
            };
            var result = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, CancellationToken.None);
            var items = result.Item2["results"] as IList<object>;
            var className = result.Item2["className"].ToString();

            IEnumerable<IObjectState> states = (from item in items
                                                select AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));

            return from state in states
                    select AVObject.FromState<T>(state, className);
        }

        #endregion

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        /// <param name="includeClassName">是否包含 ClassName </param>
        /// <returns></returns>
        public IDictionary<string, object> BuildParameters(bool includeClassName = false) {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (condition != null) {
                result["where"] = condition.ToJSON();
            }
            if (orderBy != null) {
                result["order"] = string.Join(",", orderBy.ToArray());
            }
            if (skip != null) {
                result["skip"] = skip.Value;
            }
            if (limit != null) {
                result["limit"] = limit.Value;
            }
            if (includes != null) {
                result["include"] = string.Join(",", includes.ToArray());
            }
            if (selectedKeys != null) {
                result["keys"] = string.Join(",", selectedKeys.ToArray());
            }
            if (includeClassName) {
                result["className"] = ClassName;
            }
            if (redirectClassNameForKey != null) {
                result["redirectClassNameForKey"] = redirectClassNameForKey;
            }
            return result;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object obj) {
            if (obj == null || !(obj is AVQuery<T>)) {
                return false;
            }

            var other = obj as AVQuery<T>;
            return ClassName.Equals(other.ClassName) &&
                   condition.Equals(other.condition) &&
                   orderBy.CollectionsEqual(other.orderBy) &&
                   includes.CollectionsEqual(other.includes) &&
                   selectedKeys.CollectionsEqual(other.selectedKeys) &&
                   Equals(skip, other.skip) &&
                   Equals(limit, other.limit);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        #region Order By

        public AVQuery<T> OrderBy(string key) {
            orderBy = new ReadOnlyCollection<string>(new List<string> { key });
            return this;
        }

        public AVQuery<T> OrderByDescending(string key) {
            orderBy = new ReadOnlyCollection<string>(new List<string> { "-" + key });
            return this;
        }

        public AVQuery<T> ThenBy(string key) {
            if (orderBy == null) {
                throw new ArgumentException("You must call OrderBy before calling ThenBy");
            }
            List<string> newOrderBy = orderBy.ToList();
            newOrderBy.Add(key);
            orderBy = new ReadOnlyCollection<string>(newOrderBy);
            return this;
        }

        public AVQuery<T> ThenByDescending(string key) {
            if (orderBy == null) {
                throw new ArgumentException("You must call OrderBy before calling ThenBy");
            }
            List<string> newOrderBy = orderBy.ToList();
            newOrderBy.Add($"-{key}");
            orderBy = new ReadOnlyCollection<string>(newOrderBy);
            return this;
        }

        #endregion

        public AVQuery<T> Include(string key) {
            includes = new ReadOnlyCollection<string>(new List<string> { key });
            return this;
        }

        public AVQuery<T> Select(string key) {
            selectedKeys = new ReadOnlyCollection<string>(new List<string> { key });
            return this;
        }

        public AVQuery<T> Skip(int count) {
            skip = count;
            return this;
        }

        public AVQuery<T> Limit(int count) {
            limit = count;
            return this;
        }

        internal AVQuery<T> RedirectClassName(string key) {
            redirectClassNameForKey = key;
            return this;
        }

        #region Where

        public AVQuery<T> WhereContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            AddCondition(key, "$in", values.ToList());
            return this;
        }

        public AVQuery<T> WhereContainsAll<TIn>(string key, IEnumerable<TIn> values) {
            AddCondition(key, "$all", values.ToList());
            return this;
        }

        public AVQuery<T> WhereContains(string key, string substring) {
            AddCondition(key, "$regex", RegexQuote(substring));
            return this;
        }

        public AVQuery<T> WhereDoesNotExist(string key) {
            AddCondition(key, "$exists", false);
            return this;
        }

        public AVQuery<T> WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query)
            where TOther : AVObject {
            AddCondition(key, "$notInQuery", query.BuildParameters(true));
            return this;
        }

        public AVQuery<T> WhereEndsWith(string key, string suffix) {
            AddCondition(key, "$regex", RegexQuote(suffix) + "$");
            return this;
        }

        public AVQuery<T> WhereEqualTo(string key, object value) {
            AddCondition(new QueryEqualCondition(key, value));
            return this;
        }

        public AVQuery<T> WhereSizeEqualTo(string key, uint size) {
            AddCondition(key, "$size", size);
            return this;
        }

        public AVQuery<T> WhereExists(string key) {
            AddCondition(key, "$exists", true);
            return this;
        }

        public AVQuery<T> WhereGreaterThan(string key, object value) {
            AddCondition(key, "$gt", value);
            return this;
        }

        public AVQuery<T> WhereGreaterThanOrEqualTo(string key, object value) {
            AddCondition(key, "$gte", value);
            return this;
        }

        public AVQuery<T> WhereLessThan(string key, object value) {
            AddCondition(key, "$lt", value);
            return this;
        }

        public AVQuery<T> WhereLessThanOrEqualTo(string key, object value) {
            AddCondition(key, "$lte", value);
            return this;
        }

        public AVQuery<T> WhereMatches(string key, Regex regex, string modifiers) {
            if (!regex.Options.HasFlag(RegexOptions.ECMAScript)) {
                throw new ArgumentException(
                  "Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.");
            }
            AddCondition(key, "$regex", regex.ToString());
            AddCondition(key, "options", modifiers);
            return this;
        }

        public AVQuery<T> WhereMatches(string key, Regex regex) {
            return WhereMatches(key, regex, null);
        }

        public AVQuery<T> WhereMatches(string key, string pattern, string modifiers) {
            return WhereMatches(key, new Regex(pattern, RegexOptions.ECMAScript), modifiers);
        }

        public AVQuery<T> WhereMatches(string key, string pattern) {
            return WhereMatches(key, pattern, null);
        }

        public AVQuery<T> WhereMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query)
            where TOther : AVObject {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(true)},
                { "key", keyInQuery}
            };
            AddCondition(key, "$select", parameters);
            return this;
        }

        public AVQuery<T> WhereDoesNotMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query)
            where TOther : AVObject {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(true)},
                { "key", keyInQuery}
            };
            AddCondition(key, "$dontSelect", parameters);
            return this;
        }

        public AVQuery<T> WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query)
            where TOther : AVObject {
            AddCondition(key, "$inQuery", query.BuildParameters(true));
            return this;
        }

        public AVQuery<T> WhereNear(string key, AVGeoPoint point) {
            AddCondition(key, "$nearSphere", point);
            return this;
        }

        public AVQuery<T> WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            AddCondition(key, "$nin", values.ToList());
            return this;
        }

        public AVQuery<T> WhereNotEqualTo(string key, object value) {
            AddCondition(key, "$ne", value);
            return this;
        }

        public AVQuery<T> WhereStartsWith(string key, string suffix) {
            AddCondition(key, "$regex", "^" + RegexQuote(suffix));
            return this;
        }

        public AVQuery<T> WhereWithinGeoBox(string key, AVGeoPoint southwest, AVGeoPoint northeast) {
            Dictionary<string, object> value = new Dictionary<string, object> {
                { "$box", new[] { southwest, northeast } }
            };
            AddCondition(key, "$within", value);
            return this;
        }

        public AVQuery<T> WhereWithinDistance(string key, AVGeoPoint point, AVGeoDistance maxDistance) {
            AddCondition(key, "$nearSphere", point);
            AddCondition(key, "$maxDistance", maxDistance.Radians);
            return this;
        }

        public AVQuery<T> WhereRelatedTo(AVObject parent, string key) {
            AddCondition(new QueryRelatedCondition(parent, key));
            return this;
        }

        #endregion

        private string RegexQuote(string input) {
            return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
        }

        public IDictionary<string, object> BuildWhere() {
            IDictionary<string, object> where = condition.ToJSON();
            return where;
        }

        void AddCondition(string key, string op, object value) {
            QueryOperationCondition cond = new QueryOperationCondition {
                Key = key,
                Op = op,
                Value = value
            };
            condition.AddCondition(cond);
        }

        void AddCondition(IQueryCondition cond) {
            condition.AddCondition(cond);
        }
    }
}
