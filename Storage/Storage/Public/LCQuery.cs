using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Query;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    /// <summary>
    /// The LCQuery class defines a query that is used to fetch LCObjects.
    /// </summary>
    public class LCQuery {
        /// <summary>
        /// The classname of this query.
        /// </summary>
        public string ClassName {
            get; internal set;
        }

        public LCCompositionalCondition Condition {
            get; internal set;
        }

        /// <summary>
        /// Constructs a LCQuery for class.
        /// </summary>
        /// <param name="className"></param>
        public LCQuery(string className) {
            ClassName = className;
            Condition = new LCCompositionalCondition();
        }

        internal Dictionary<string, object> BuildParams() {
            return Condition.BuildParams();
        }

        internal string BuildWhere() {
            return Condition.BuildWhere();
        }
    }

    /// <summary>
    /// A query to fetch LCObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LCQuery<T> : LCQuery where T : LCObject {
        public LCQuery(string className) :
            base(className) {

        }

        /// <summary>
        /// The value corresponding to key is equal to value, or the array corresponding to key contains value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereEqualTo(string key, object value)  {
            Condition.WhereEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is not equal to value, or the array corresponding to key does not contain value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereNotEqualTo(string key, object value) {
            Condition.WhereNotEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// Values contains value corresponding to key, or values contains at least one element in the array corresponding to key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCQuery<T> WhereContainedIn(string key, IEnumerable values) {
            Condition.WhereContainedIn(key, values);
            return this;
        }

        /// <summary>
        /// The value of key must not be contained in values.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCQuery<T> WhereNotContainedIn(string key, IEnumerable values) {
            Condition.WhereNotContainedIn(key, values);
            return this;
        }

        /// <summary>
        /// The array corresponding to key contains all elements in values.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCQuery<T> WhereContainsAll(string key, IEnumerable values) {
            Condition.WhereContainsAll(key, values);
            return this;
        }

        /// <summary>
        /// The attribute corresponding to key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> WhereExists(string key) {
            Condition.WhereExists(key);
            return this;
        }

        /// <summary>
        /// The attribute corresponding to key does not exist. 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> WhereDoesNotExist(string key) {
            Condition.WhereDoesNotExist(key);
            return this;
        }

        /// <summary>
        /// The size of the array corresponding to key is equal to size.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public LCQuery<T> WhereSizeEqualTo(string key, int size) {
            Condition.WhereSizeEqualTo(key, size);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is greater than value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereGreaterThan(string key, object value) {
            Condition.WhereGreaterThan(key, value);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is greater than or equal to value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereGreaterThanOrEqualTo(string key, object value) {
            Condition.WhereGreaterThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is less than value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereLessThan(string key, object value) {
            Condition.WhereLessThan(key, value);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is less than or equal to value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereLessThanOrEqualTo(string key, object value) {
            Condition.WhereLessThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is near the point.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public LCQuery<T> WhereNear(string key, LCGeoPoint point) {
            Condition.WhereNear(key, point);
            return this;
        }

        /// <summary>
        /// The value corresponding to key is in the given rectangular geographic bounding box.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="southwest"></param>
        /// <param name="northeast"></param>
        /// <returns></returns>
        public LCQuery<T> WhereWithinGeoBox(string key, LCGeoPoint southwest, LCGeoPoint northeast) {
            Condition.WhereWithinGeoBox(key, southwest, northeast);
            return this;
        }

        /// <summary>
        ///  The value corresponding to key is related to the parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> WhereRelatedTo(LCObject parent, string key) {
            Condition.WhereRelatedTo(parent, key);
            return this;
        }

        /// <summary>
        /// The string corresponding to key has a prefix.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public LCQuery<T> WhereStartsWith(string key, string prefix) {
            Condition.WhereStartsWith(key, prefix);
            return this;
        }

        /// <summary>
        /// The string corresponding to key has a suffix.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public LCQuery<T> WhereEndsWith(string key, string suffix) {
            Condition.WhereEndsWith(key, suffix);
            return this;
        }

        /// <summary>
        /// The string corresponding to key has a subString.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subString"></param>
        /// <returns></returns>
        public LCQuery<T> WhereContains(string key, string subString) {
            Condition.WhereContains(key, subString);
            return this;
        }

        /// <summary>
        /// Matches the regexp.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="regex"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        public LCQuery<T> WhereMatches(string key, string regex, string modifiers = null) {
            Condition.WhereMatches(key, regex, modifiers);
            return this;
        }

        /// <summary>
        /// The value of key must match query.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public LCQuery<T> WhereMatchesQuery<K>(string key, LCQuery<K> query) where K : LCObject {
            Condition.WhereMatchesQuery(key, query);
            return this;
        }

        /// <summary>
        /// The value of key must not match query.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="key"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public LCQuery<T> WhereDoesNotMatchQuery<K>(string key, LCQuery<K> query) where K : LCObject {
            Condition.WhereDoesNotMatchQuery(key, query);
            return this;
        }

        /// <summary>
        /// Sorts the results in ascending order by the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> OrderByAscending(string key) {
            Condition.OrderByAscending(key);
            return this;
        }

        /// <summary>
        /// Sorts the results in descending order by the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> OrderByDescending(string key) {
            Condition.OrderByDescending(key);
            return this;
        }

        /// <summary>
        /// Also sorts the results in ascending order by the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> AddAscendingOrder(string key) {
            Condition.AddAscendingOrder(key);
            return this;
        }

        /// <summary>
        /// Also sorts the results in descending order by the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> AddDescendingOrder(string key) {
            Condition.AddDescendingOrder(key);
            return this;
        }

        /// <summary>
        /// Includes nested LCObject for the provided key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> Include(string key) {
            Condition.Include(key);
            return this;
        }

        /// <summary>
        /// Restricts the keys of the LCObject returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> Select(string key) {
            Condition.Select(key);
            return this;
        }

        /// <summary>
        /// Includes the ALC or not.
        /// </summary>
        public bool IncludeACL {
            get {
                return Condition.IncludeACL;
            } set {
                Condition.IncludeACL = value;
            }
        }

        /// <summary>
        /// Sets the amount of results to skip before returning any results.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> Skip(int value) {
            Condition.Skip = value;
            return this;
        }

        /// <summary>
        /// Sets the limit of the number of results to return.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> Limit(int value) {
            Condition.Limit = value;
            return this;
        }

        /// <summary>
        /// Counts the number of objects that match this query.
        /// </summary>
        /// <returns></returns>
        public async Task<int> Count() {
            string path = $"classes/{ClassName}";
            Dictionary<string, object> parameters = BuildParams();
            parameters["limit"] = 0;
            parameters["count"] = 1;
            Dictionary<string, object> ret = await LCCore.HttpClient.Get<Dictionary<string, object>>(path, queryParams: parameters);
            return (int)ret["count"];
        }

        /// <summary>
        /// Constructs a LCObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        public async Task<T> Get(string objectId) {
            if (string.IsNullOrEmpty(objectId)) {
                throw new ArgumentNullException(nameof(objectId));
            }
            string path = $"classes/{ClassName}/{objectId}";
            Dictionary<string, object> queryParams = null;
            string includes = Condition.BuildIncludes();
            if (!string.IsNullOrEmpty(includes)) {
                queryParams = new Dictionary<string, object> {
                    { "include", includes }
                };
            }
            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>(path, queryParams: queryParams);
            return DecodeLCObject(response);
        }

        /// <summary>
        /// Retrieves a list of LCObjects that satisfy the query from Server.
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<T>> Find() {
            string path = $"classes/{ClassName}";
            Dictionary<string, object> parameters = BuildParams();
            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>(path, queryParams: parameters);
            List<object> results = response["results"] as List<object>;
            List<T> list = new List<T>();
            foreach (object item in results) {
                T obj = DecodeLCObject(item as Dictionary<string, object>);
                list.Add(obj);
            }
            return list.AsReadOnly();
        }

        /// <summary>
        /// Retrieves at most one LCObject that satisfies this query.
        /// </summary>
        /// <returns></returns>
        public async Task<T> First() {
            Limit(1);
            ReadOnlyCollection<T> results = await Find();
            if (results != null && results.Count > 0) {
                return results[0];
            }
            return null;
        }

        /// <summary>
        /// Constructs a query that is the and of the given queries.
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        public static LCQuery<T> And(IEnumerable<LCQuery<T>> queries) {
            if (queries == null || queries.Count() < 1) {
                throw new ArgumentNullException(nameof(queries));
            }
            LCQuery<T> compositionQuery = new LCQuery<T>(null);
            string className = null;
            foreach (LCQuery<T> query in queries) {
                if (className != null && className != query.ClassName) {
                    throw new Exception("All of the queries in an or query must be on the same class.");
                }
                className = query.ClassName;
                compositionQuery.Condition.Add(query.Condition);
            }
            compositionQuery.ClassName = className;
            return compositionQuery;
        }

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        public static LCQuery<T> Or(IEnumerable<LCQuery<T>> queries) {
            if (queries == null || queries.Count() < 1) {
                throw new ArgumentNullException(nameof(queries));
            }
            LCQuery<T> compositionQuery = new LCQuery<T>(null);
            compositionQuery.Condition = new LCCompositionalCondition(LCCompositionalCondition.Or);
            string className = null;
            foreach (LCQuery<T> query in queries) {
                if (className != null && className != query.ClassName) {
                    throw new Exception("All of the queries in an or query must be on the same class.");
                }
                className = query.ClassName;
                compositionQuery.Condition.Add(query.Condition);
            }
            compositionQuery.ClassName = className;
            return compositionQuery;
        }

        private T DecodeLCObject(Dictionary<string, object> data) {
            LCObjectData objectData = LCObjectData.Decode(data);
            T obj = LCObject.Create(ClassName) as T;
            obj.Merge(objectData);
            return obj;
        }
    }
}
