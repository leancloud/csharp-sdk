using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal.Query;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    /// <summary>
    /// 查询类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LCQuery<T> where T : LCObject {
        public string ClassName {
            get; private set;
        }

        LCCompositionalCondition condition;

        public LCQuery(string className) {
            ClassName = className;
            condition = new LCCompositionalCondition();
        }

        /// <summary>
        /// 等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereEqualTo(string key, object value) {
            condition.WhereEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 不等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereNotEqualTo(string key, object value) {
            condition.WhereNotEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCQuery<T> WhereContainedIn(string key, IEnumerable values) {
            condition.WhereContainedIn(key, values);
            return this;
        }

        /// <summary>
        /// 包含全部
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCQuery<T> WhereContainsAll(string key, IEnumerable values) {
            condition.WhereContainsAll(key, values);
            return this;
        }

        /// <summary>
        /// 存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> WhereExists(string key) {
            condition.WhereExists(key);
            return this;
        }

        /// <summary>
        /// 不存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> WhereDoesNotExist(string key) {
            condition.WhereDoesNotExist(key);
            return this;
        }

        /// <summary>
        /// 长度等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public LCQuery<T> WhereSizeEqualTo(string key, int size) {
            condition.WhereSizeEqualTo(key, size);
            return this;
        }

        /// <summary>
        /// 大于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereGreaterThan(string key, object value) {
            condition.WhereGreaterThan(key, value);
            return this;
        }

        /// <summary>
        /// 大于等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereGreaterThanOrEqualTo(string key, object value) {
            condition.WhereGreaterThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 小于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereLessThan(string key, object value) {
            condition.WhereLessThan(key, value);
            return this;
        }

        /// <summary>
        /// 小于等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> WhereLessThanOrEqualTo(string key, object value) {
            condition.WhereLessThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 相邻
        /// </summary>
        /// <param name="key"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public LCQuery<T> WhereNear(string key, LCGeoPoint point) {
            condition.WhereNear(key, point);
            return this;
        }

        /// <summary>
        /// 在坐标区域内
        /// </summary>
        /// <param name="key"></param>
        /// <param name="southwest"></param>
        /// <param name="northeast"></param>
        /// <returns></returns>
        public LCQuery<T> WhereWithinGeoBox(string key, LCGeoPoint southwest, LCGeoPoint northeast) {
            condition.WhereWithinGeoBox(key, southwest, northeast);
            return this;
        }

        /// <summary>
        /// 相关
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> WhereRelatedTo(LCObject parent, string key) {
            condition.WhereRelatedTo(parent, key);
            return this;
        }

        /// <summary>
        /// 前缀
        /// </summary>
        /// <param name="key"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public LCQuery<T> WhereStartsWith(string key, string prefix) {
            condition.WhereStartsWith(key, prefix);
            return this;
        }

        /// <summary>
        /// 后缀
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public LCQuery<T> WhereEndsWith(string key, string suffix) {
            condition.WhereEndsWith(key, suffix);
            return this;
        }

        /// <summary>
        /// 字符串包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subString"></param>
        /// <returns></returns>
        public LCQuery<T> WhereContains(string key, string subString) {
            condition.WhereContains(key, subString);
            return this;
        }

        /// <summary>
        /// 按 key 升序
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> OrderBy(string key) {
            condition.OrderBy(key);
            return this;
        }

        /// <summary>
        /// 按 key 降序
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> OrderByDescending(string key) {
            condition.OrderByDescending(key);
            return this;
        }

        /// <summary>
        /// 拉取 key 的完整对象
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> Include(string key) {
            condition.Include(key);
            return this;
        }

        /// <summary>
        /// 包含 key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCQuery<T> Select(string key) {
            condition.Select(key);
            return this;
        }

        /// <summary>
        /// 跳过
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> Skip(int value) {
            condition.Skip = value;
            return this;
        }

        /// <summary>
        /// 限制数量
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCQuery<T> Limit(int value) {
            condition.Limit = value;
            return this;
        }

        public async Task<int> Count() {
            string path = $"classes/{ClassName}";
            Dictionary<string, object> parameters = BuildParams();
            parameters["limit"] = 0;
            parameters["count"] = 1;
            Dictionary<string, object> ret = await LCApplication.HttpClient.Get<Dictionary<string, object>>(path, queryParams: parameters);
            return (int)ret["count"];
        }

        public async Task<T> Get(string objectId) {
            if (string.IsNullOrEmpty(objectId)) {
                throw new ArgumentNullException(nameof(objectId));
            }
            WhereEqualTo("objectId", objectId);
            Limit(1);
            List<T> results = await Find();
            if (results != null) {
                if (results.Count == 0) {
                    return null;
                }
                return results[0];
            }
            return null;
        }

        public async Task<List<T>> Find() {
            string path = $"classes/{ClassName}";
            Dictionary<string, object> parameters = BuildParams();
            Dictionary<string, object> response = await LCApplication.HttpClient.Get<Dictionary<string, object>>(path, queryParams: parameters);
            List<object> results = response["results"] as List<object>;
            List<T> list = new List<T>();
            foreach (object item in results) {
                LCObjectData objectData = LCObjectData.Decode(item as Dictionary<string, object>);
                T obj = LCObject.Create(ClassName) as T;
                obj.Merge(objectData);
                list.Add(obj);
            }
            return list;
        }

        public async Task<T> First() {
            Limit(1);
            List<T> results = await Find();
            if (results != null && results.Count > 0) {
                return results[0];
            }
            return null;
        }

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
                compositionQuery.condition.Add(query.condition);
            }
            compositionQuery.ClassName = className;
            return compositionQuery;
        }

        public static LCQuery<T> Or(IEnumerable<LCQuery<T>> queries) {
            if (queries == null || queries.Count() < 1) {
                throw new ArgumentNullException(nameof(queries));
            }
            LCQuery<T> compositionQuery = new LCQuery<T>(null);
            compositionQuery.condition = new LCCompositionalCondition(LCCompositionalCondition.Or);
            string className = null;
            foreach (LCQuery<T> query in queries) {
                if (className != null && className != query.ClassName) {
                    throw new Exception("All of the queries in an or query must be on the same class.");
                }
                className = query.ClassName;
                compositionQuery.condition.Add(query.condition);
            }
            compositionQuery.ClassName = className;
            return compositionQuery;
        }

        Dictionary<string, object> BuildParams() {
            return condition.BuildParams();
        }

        internal string BuildWhere() {
            return condition.BuildWhere();
        }
    }
}
