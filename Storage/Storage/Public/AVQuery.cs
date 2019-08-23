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

namespace LeanCloud
{
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
            } set {
                path = value;
            }
        }

        internal IDictionary<string, object> where;
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
        }

        private AVQuery(AVQuery<T> source,
            IDictionary<string, object> where = null,
            IEnumerable<string> replacementOrderBy = null,
            IEnumerable<string> thenBy = null,
            int? skip = null,
            int? limit = null,
            IEnumerable<string> includes = null,
            IEnumerable<string> selectedKeys = null,
            string redirectClassNameForKey = null) {

            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            ClassName = source.ClassName;
            this.where = source.where;
            this.orderBy = source.orderBy;
            this.skip = source.skip;
            this.limit = source.limit;
            this.includes = source.includes;
            this.selectedKeys = source.selectedKeys;
            this.redirectClassNameForKey = source.redirectClassNameForKey;

            if (where != null) {
                var newWhere = MergeWhereClauses(where);
                this.where = new Dictionary<string, object>(newWhere);
            }

            if (replacementOrderBy != null) {
                this.orderBy = new ReadOnlyCollection<string>(replacementOrderBy.ToList());
            }

            if (thenBy != null) {
                if (this.orderBy == null) {
                    throw new ArgumentException("You must call OrderBy before calling ThenBy.");
                }
                var newOrderBy = new List<string>(this.orderBy);
                newOrderBy.AddRange(thenBy);
                this.orderBy = new ReadOnlyCollection<string>(newOrderBy);
            }

            // Remove duplicates.
            if (this.orderBy != null) {
                var newOrderBy = new HashSet<string>(this.orderBy);
                this.orderBy = new ReadOnlyCollection<string>(newOrderBy.ToList<string>());
            }

            if (skip != null) {
                this.skip = (this.skip ?? 0) + skip;
            }

            if (limit != null) {
                this.limit = limit;
            }

            if (includes != null) {
                var newIncludes = MergeIncludes(includes);
                this.includes = new ReadOnlyCollection<string>(newIncludes.ToList());
            }

            if (selectedKeys != null) {
                var newSelectedKeys = MergeSelectedKeys(selectedKeys);
                this.selectedKeys = new ReadOnlyCollection<string>(newSelectedKeys.ToList());
            }

            if (redirectClassNameForKey != null) {
                this.redirectClassNameForKey = redirectClassNameForKey;
            }
        }

        HashSet<string> MergeIncludes(IEnumerable<string> otherIncludes) {
            if (includes == null) {
                return new HashSet<string>(otherIncludes);
            }
            var newIncludes = new HashSet<string>(includes);
            foreach (var item in otherIncludes) {
                newIncludes.Add(item);
            }
            return newIncludes;
        }

        HashSet<string> MergeSelectedKeys(IEnumerable<string> otherSelectedKeys) {
            if (selectedKeys == null) {
                return new HashSet<string>(otherSelectedKeys);
            }
            var newSelectedKeys = new HashSet<string>(selectedKeys);
            foreach (var item in otherSelectedKeys) {
                newSelectedKeys.Add(item);
            }
            return newSelectedKeys;
        }

        public static AVQuery<T> Or(IEnumerable<AVQuery<T>> queries) {
            string className = null;
            var orValue = new List<IDictionary<string, object>>();
            // We need to cast it to non-generic IEnumerable because of AOT-limitation
            var nonGenericQueries = (IEnumerable)queries;
            foreach (var obj in nonGenericQueries) {
                var q = (AVQuery<T>)obj;
                if (className != null && q.ClassName != className) {
                    throw new ArgumentException("All of the queries in an or query must be on the same class.");
                }
                className = q.ClassName;
                var parameters = q.BuildParameters();
                if (parameters.Count == 0) {
                    continue;
                }
                if (!parameters.TryGetValue("where", out object where) || parameters.Count > 1) {
                    throw new ArgumentException("None of the queries in an or query can have non-filtering clauses");
                }
                orValue.Add(where as IDictionary<string, object>);
            }
            return new AVQuery<T>(new AVQuery<T>(className), new Dictionary<string, object> {
                  { "$or", orValue }
            });
        }

        public static AVQuery<T> And(IEnumerable<AVQuery<T>> queries) {
            string className = null;
            var andValue = new List<IDictionary<string, object>>();
            // We need to cast it to non-generic IEnumerable because of AOT-limitation
            var nonGenericQueries = (IEnumerable)queries;
            foreach (var obj in nonGenericQueries) {
                var q = (AVQuery<T>)obj;
                if (className != null && q.ClassName != className) {
                    throw new ArgumentException("All of the queries in an or query must be on the same class.");
                }
                className = q.ClassName;
                var parameters = q.BuildParameters();
                if (parameters.Count == 0) {
                    continue;
                }
                if (!parameters.TryGetValue("where", out object where) || parameters.Count > 1) {
                    throw new ArgumentException("None of the queries in an or query can have non-filtering clauses");
                }
                andValue.Add(where as IDictionary<string, object>);
            }
            return new AVQuery<T>(new AVQuery<T>(className), new Dictionary<string, object> {
                  { "$and", andValue }
            });
        }

        public Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken = default)
        {
            return QueryController.FindAsync<T>(this, AVUser.CurrentUser, cancellationToken).OnSuccess(t => {
                IEnumerable<IObjectState> states = t.Result;
                return (from state in states
                        select AVObject.FromState<T>(state, ClassName));
            });
        }

        public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        {
            return QueryController.FirstAsync<T>(this, AVUser.CurrentUser, cancellationToken).OnSuccess(t => {
                IObjectState state = t.Result;
                return state == null ? default : AVObject.FromState<T>(state, ClassName);
            });
        }

        public Task<T> FirstAsync(CancellationToken cancellationToken)
        {
            return FirstOrDefaultAsync(cancellationToken).OnSuccess(t =>
            {
                if (t.Result == null)
                {
                    throw new AVException(AVException.ErrorCode.ObjectNotFound,
                      "No results matched the query.");
                }
                return t.Result;
            });
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return QueryController.CountAsync(this, AVUser.CurrentUser, cancellationToken);
        }

        public Task<T> GetAsync(string objectId, CancellationToken cancellationToken)
        {
            AVQuery<T> singleItemQuery = new AVQuery<T>(ClassName)
                .WhereEqualTo("objectId", objectId);
            singleItemQuery = new AVQuery<T>(singleItemQuery, includes: this.includes, selectedKeys: this.selectedKeys, limit: 1);
            return singleItemQuery.FindAsync(cancellationToken).OnSuccess(t =>
            {
                var result = t.Result.FirstOrDefault();
                if (result == null)
                {
                    throw new AVException(AVException.ErrorCode.ObjectNotFound,
                      "Object with the given objectId not found.");
                }
                return result;
            });
        }

        #region CQL
        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql">CQL 语句</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>返回符合条件的对象集合</returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql, CancellationToken cancellationToken) {
            var queryString = $"cloudQuery?cql={Uri.EscapeDataString(cql)}";
            return RebuildObjectFromCloudQueryResult(queryString);
        }

        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql"></param>
        /// <returns></returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql) {
            return DoCloudQueryAsync(cql, CancellationToken.None);
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

        internal static Task<IEnumerable<T>> RebuildObjectFromCloudQueryResult(string queryString) {
            var command = new AVCommand {
                Path = queryString,
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: CancellationToken.None).OnSuccess(t =>
            {
                var items = t.Result.Item2["results"] as IList<object>;
                var className = t.Result.Item2["className"].ToString();

                IEnumerable<IObjectState> states = (from item in items
                                                    select AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));

                return (from state in states
                        select AVObject.FromState<T>(state, className));
            });
        }

        #endregion

        IDictionary<string, object> MergeWhereClauses(IDictionary<string, object> otherWhere) {
            if (where == null) {
                where = otherWhere;
                return where;
            }
            var newWhere = new Dictionary<string, object>(where);
            foreach (var pair in otherWhere) {
                var condition = pair.Value as IDictionary<string, object>;
                if (newWhere.ContainsKey(pair.Key)) {
                    var oldCondition = newWhere[pair.Key] as IDictionary<string, object>;
                    if (oldCondition == null || condition == null) {
                        throw new ArgumentException("More than one where clause for the given key provided.");
                    }
                    var newCondition = new Dictionary<string, object>(oldCondition);
                    foreach (var conditionPair in condition) {
                        if (newCondition.ContainsKey(conditionPair.Key)) {
                            throw new ArgumentException("More than one condition for the given key provided.");
                        }
                        newCondition[conditionPair.Key] = conditionPair.Value;
                    }
                    newWhere[pair.Key] = newCondition;
                } else {
                    newWhere[pair.Key] = pair.Value;
                }
            }
            where = newWhere;
            return where;
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        /// <param name="includeClassName">是否包含 ClassName </param>
        /// <returns></returns>
        public IDictionary<string, object> BuildParameters(bool includeClassName = false) {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (where != null) {
                result["where"] = PointerOrLocalIdEncoder.Instance.Encode(where);
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
                   where.CollectionsEqual(other.where) &&
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
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$in", values.ToList() } } }
            });
            return this;
        }

        public AVQuery<T> WhereContainsAll<TIn>(string key, IEnumerable<TIn> values) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$all", values.ToList() } } }
            });
            return this;
        }

        public AVQuery<T> WhereContains(string key, string substring) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$regex", RegexQuote(substring) } } }
            });
            return this;
        }

        public AVQuery<T> WhereDoesNotExist(string key) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$exists", false } } }
            });
            return this;
        }

        public AVQuery<T> WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query)
            where TOther : AVObject {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$notInQuery", query.BuildParameters(true) } } }
            });
            return this;
        }

        public AVQuery<T> WhereEndsWith(string key, string suffix) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$regex", RegexQuote(suffix) + "$" } } }
            });
            return this;
        }

        public AVQuery<T> WhereEqualTo(string key, object value) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, value }
            });
            return this;
        }

        public AVQuery<T> WhereSizeEqualTo(string key, uint size) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$size", size } } }
            });
            return this;
        }

        public AVQuery<T> WhereExists(string key) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$exists", true } } }
            });
            return this;
        }

        public AVQuery<T> WhereGreaterThan(string key, object value) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$gt", value } } }
            });
            return this;
        }

        public AVQuery<T> WhereGreaterThanOrEqualTo(string key, object value) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$gte", value } } }
            });
            return this;
        }

        public AVQuery<T> WhereLessThan(string key, object value) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$lt", value } } }
            });
            return this;
        }

        public AVQuery<T> WhereLessThanOrEqualTo(string key, object value) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$lte", value } } }
            });
            return this;
        }

        public AVQuery<T> WhereMatches(string key, Regex regex, string modifiers) {
            if (!regex.Options.HasFlag(RegexOptions.ECMAScript)) {
                throw new ArgumentException(
                  "Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.");
            }
            MergeWhereClauses(new Dictionary<string, object> {
                { key, EncodeRegex(regex, modifiers) }
            });
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
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$select", parameters } } }
            });
            return this;
        }

        public AVQuery<T> WhereDoesNotMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query)
            where TOther : AVObject {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(true)},
                { "key", keyInQuery}
            };
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$dontSelect", parameters } } }
            });
            return this;
        }

        public AVQuery<T> WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query)
            where TOther : AVObject {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$inQuery", query.BuildParameters(true) } } }
            });
            return this;
        }

        public AVQuery<T> WhereNear(string key, AVGeoPoint point) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$nearSphere", point } } }
            });
            return this;
        }

        public AVQuery<T> WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$nin", values.ToList() } } }
            });
            return this;
        }

        public AVQuery<T> WhereNotEqualTo(string key, object value) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$ne", value } } }
            });
            return this;
        }

        public AVQuery<T> WhereStartsWith(string key, string suffix) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$regex", "^" + RegexQuote(suffix) } } }
            });
            return this;
        }

        public AVQuery<T> WhereWithinGeoBox(string key, AVGeoPoint southwest, AVGeoPoint northeast) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$within",
                            new Dictionary<string, object> {
                                { "$box", new[] {southwest, northeast}}
                            } } } }
            });
            return this;
        }

        public AVQuery<T> WhereWithinDistance(string key, AVGeoPoint point, AVGeoDistance maxDistance) {
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$nearSphere", point } } }
            });
            MergeWhereClauses(new Dictionary<string, object> {
                { key, new Dictionary<string, object>{ { "$maxDistance", maxDistance.Radians } } }
            });
            return this;
        }

        internal AVQuery<T> WhereRelatedTo(AVObject parent, string key) {
            MergeWhereClauses(new Dictionary<string, object> {
                {
                    "$relatedTo",
                    new Dictionary<string, object> {
                        { "object", parent },
                        { "key", key }
                    }
                }
            });
            return this;
        }

        #endregion

        private string RegexQuote(string input) {
            return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
        }

        private IDictionary<string, object> EncodeRegex(Regex regex, string modifiers) {
            var options = GetRegexOptions(regex, modifiers);
            var dict = new Dictionary<string, object>();
            dict["$regex"] = regex.ToString();
            if (!string.IsNullOrEmpty(options)) {
                dict["$options"] = options;
            }
            return dict;
        }

        private string GetRegexOptions(Regex regex, string modifiers) {
            string result = modifiers ?? "";
            if (regex.Options.HasFlag(RegexOptions.IgnoreCase) && !modifiers.Contains("i")) {
                result += "i";
            }
            if (regex.Options.HasFlag(RegexOptions.Multiline) && !modifiers.Contains("m")) {
                result += "m";
            }
            return result;
        }
    }
}
