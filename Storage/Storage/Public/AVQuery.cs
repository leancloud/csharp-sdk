using System;
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

        static AVQueryController QueryController {
            get {
                return AVPlugins.Instance.QueryController;
            }
        }

        public AVQuery()
            : this(AVPlugins.Instance.SubclassingController.GetClassName(typeof(T))) {
        }

        public AVQuery(string className) {
            if (string.IsNullOrEmpty(className)) {
                throw new ArgumentNullException(nameof(className));
            }
            ClassName = className;
            condition = new QueryCompositionalCondition();
        }

        #region Composition

        public static AVQuery<T> And(IEnumerable<AVQuery<T>> queries) {
            AVQuery<T> composition = new AVQuery<T>();
            string className = null;
            if (queries != null) {
                foreach (AVQuery<T> query in queries) {
                    if (className != null && className != query.ClassName) {
                        throw new ArgumentException("All of the queries in an or query must be on the same class.");
                    }
                    composition.condition.AddCondition(query.condition);
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
                    composition.condition.AddCondition(query.condition);
                    className = query.ClassName;
                }
            }
            composition.ClassName = className;
            return composition;
        }

        #endregion

        public virtual async Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken = default) {
            IEnumerable<IObjectState> states = await QueryController.FindAsync(this, cancellationToken);
            return (from state in states
                    select AVObject.FromState<T>(state, ClassName));
        }

        public virtual async Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default) {
            IObjectState state = await QueryController.FirstAsync<T>(this, cancellationToken);
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
            return QueryController.CountAsync(this, cancellationToken);
        }

        public virtual async Task<T> GetAsync(string objectId, CancellationToken cancellationToken) {
            WhereEqualTo("objectId", objectId);
            Limit(1);
            var result = await FindAsync(cancellationToken);
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
                   condition.Equals(other.condition);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        #region Order By

        public AVQuery<T> OrderBy(string key) {
            condition.OrderBy(key);
            return this;
        }

        public AVQuery<T> OrderByDescending(string key) {
            condition.OrderByDescending(key);
            return this;
        }

        #endregion

        public AVQuery<T> Include(string key) {
            condition.Include(key);
            return this;
        }

        public AVQuery<T> Select(string key) {
            condition.Select(key);
            return this;
        }

        public AVQuery<T> Skip(int count) {
            condition.Skip(count);
            return this;
        }

        public AVQuery<T> Limit(int count) {
            condition.Limit(count);
            return this;
        }

        #region Where

        public AVQuery<T> WhereContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            condition.WhereContainedIn(key, values);
            return this;
        }

        public AVQuery<T> WhereContainsAll<TIn>(string key, IEnumerable<TIn> values) {
            condition.WhereContainsAll(key, values);
            return this;
        }

        public AVQuery<T> WhereContains(string key, string substring) {
            condition.WhereContains(key, substring);
            return this;
        }

        public AVQuery<T> WhereDoesNotExist(string key) {
            condition.WhereDoesNotExist(key);
            return this;
        }

        public AVQuery<T> WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereDoesNotMatchQuery(key, query);
            return this;
        }

        public AVQuery<T> WhereEndsWith(string key, string suffix) {
            condition.WhereEndsWith(key, suffix);
            return this;
        }

        public AVQuery<T> WhereEqualTo(string key, object value) {
            condition.WhereEqualTo(key, value);
            return this;
        }

        public AVQuery<T> WhereSizeEqualTo(string key, uint size) {
            condition.WhereSizeEqualTo(key, size);
            return this;
        }

        public AVQuery<T> WhereExists(string key) {
            condition.WhereExists(key);
            return this;
        }

        public AVQuery<T> WhereGreaterThan(string key, object value) {
            condition.WhereGreaterThan(key, value);
            return this;
        }

        public AVQuery<T> WhereGreaterThanOrEqualTo(string key, object value) {
            condition.WhereGreaterThanOrEqualTo(key, value);
            return this;
        }

        public AVQuery<T> WhereLessThan(string key, object value) {
            condition.WhereLessThan(key, value);
            return this;
        }

        public AVQuery<T> WhereLessThanOrEqualTo(string key, object value) {
            condition.WhereLessThanOrEqualTo(key, value);
            return this;
        }

        public AVQuery<T> WhereMatches(string key, Regex regex, string modifiers) {
            condition.WhereMatches(key, regex, modifiers);
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

        public AVQuery<T> WhereMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereMatchesKeyInQuery(key, keyInQuery, query);
            return this;
        }

        public AVQuery<T> WhereDoesNotMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereDoesNotMatchesKeyInQuery(key, keyInQuery, query);
            return this;
        }

        public AVQuery<T> WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereMatchesQuery(key, query);
            return this;
        }

        public AVQuery<T> WhereNear(string key, AVGeoPoint point) {
            condition.WhereNear(key, point);
            return this;
        }

        public AVQuery<T> WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            condition.WhereNotContainedIn(key, values);
            return this;
        }

        public AVQuery<T> WhereNotEqualTo(string key, object value) {
            condition.WhereNotEqualTo(key, value);
            return this;
        }

        public AVQuery<T> WhereStartsWith(string key, string suffix) {
            condition.WhereStartsWith(key, suffix);
            return this;
        }

        public AVQuery<T> WhereWithinGeoBox(string key, AVGeoPoint southwest, AVGeoPoint northeast) {
            condition.WhereWithinGeoBox(key, southwest, northeast);
            return this;
        }

        public AVQuery<T> WhereWithinDistance(string key, AVGeoPoint point, AVGeoDistance maxDistance) {
            condition.WhereWithinDistance(key, point, maxDistance);
            return this;
        }

        public AVQuery<T> WhereRelatedTo(AVObject parent, string key) {
            condition.WhereRelatedTo(parent, key);
            return this;
        }

        #endregion

        internal IDictionary<string, object> BuildParameters(string className = null) {
            return condition.BuildParameters(className);
        }

        public IDictionary<string, object> BuildWhere() {
            return condition.ToJSON();
        }
    }
}
