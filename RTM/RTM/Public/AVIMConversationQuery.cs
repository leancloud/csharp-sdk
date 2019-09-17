using LeanCloud.Realtime.Internal;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 对话查询类
    /// </summary>
    public class AVIMConversationQuery {
        internal AVIMClient CurrentClient { get; set; }
        
        QueryCombinedCondition condition;

        public AVIMConversationQuery() {
            condition = new QueryCombinedCondition();
        }

        internal ConversationCommand GenerateQueryCommand() {
            var cmd = new ConversationCommand();

            var queryParameters = BuildParameters();
            if (queryParameters != null) {
                if (queryParameters.TryGetValue("where", out object where)) {
                    cmd.Where(where);
                }
                if (queryParameters.TryGetValue("skip", out object skip)) {
                    cmd.Skip((int)skip);
                }
                if (queryParameters.TryGetValue("limit", out object limit)) {
                    cmd.Limit((int)limit);
                }
                if (queryParameters.TryGetValue("order", out object order)) {
                    cmd.Sort(order.ToString());
                }
            }

            return cmd;
        }

        #region Combined Query

        public static AVIMConversationQuery And(IEnumerable<AVIMConversationQuery> queries) {
            AVIMConversationQuery composition = new AVIMConversationQuery();
            if (queries != null) {
                foreach (AVIMConversationQuery query in queries) {
                    composition.condition.AddCondition(query.condition);
                }
            }
            return composition;
        }

        public static AVIMConversationQuery Or(IEnumerable<AVIMConversationQuery> queries) {
            AVIMConversationQuery composition = new AVIMConversationQuery {
                condition = new QueryCombinedCondition(QueryCombinedCondition.OR)
            };
            if (queries != null) {
                foreach (AVIMConversationQuery query in queries) {
                    composition.condition.AddCondition(query.condition);
                }
            }
            return composition;
        }

        #endregion

        public Task<int> CountAsync() {
            var convCmd = GenerateQueryCommand();
            convCmd.Count();
            convCmd.Limit(0);
            var cmd = convCmd.Option("query");
            return CurrentClient.RunCommandAsync(convCmd).OnSuccess(t => {
                var result = t.Result.Item2;

                if (result.ContainsKey("count")) {
                    return int.Parse(result["count"].ToString());
                }
                return 0;
            });
        }


        /// <summary>
        /// 查找符合条件的对话
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<AVIMConversation>> FindAsync() {
            var convCmd = GenerateQueryCommand().Option("query");
            return CurrentClient.RunCommandAsync(convCmd).OnSuccess(t => {
                var result = t.Result.Item2;

                IList<AVIMConversation> rtn = new List<AVIMConversation>();
                if (result["results"] is IList<object> conList) {
                    foreach (var c in conList) {
                        if (c is IDictionary<string, object> cData) {
                            var con = AVIMConversation.CreateWithData(cData, CurrentClient);
                            rtn.Add(con);
                        }
                    }
                }
                return rtn.AsEnumerable();
            });
        }

        public Task<AVIMConversation> FirstAsync() {
            return FirstOrDefaultAsync();
        }

        public Task<AVIMConversation> FirstOrDefaultAsync() {
            var firstQuery = Limit(1);
            return firstQuery.FindAsync().OnSuccess(t => {
                return t.Result.FirstOrDefault();
            });
        }

        public Task<AVIMConversation> GetAsync(string objectId) {
            var idQuery = WhereEqualTo("objectId", objectId);
            return idQuery.FirstAsync();
        }

        public AVIMConversationQuery OrderBy(string key) {
            condition.OrderBy(key);
            return this;
        }

        public AVIMConversationQuery OrderByDescending(string key) {
            condition.OrderByDescending(key);
            return this;
        }

        public AVIMConversationQuery Include(string key) {
            condition.Include(key);
            return this;
        }

        public AVIMConversationQuery Select(string key) {
            condition.Select(key);
            return this;
        }

        public AVIMConversationQuery Skip(int count) {
            condition.Skip(count);
            return this;
        }

        public AVIMConversationQuery Limit(int count) {
            condition.Limit(count);
            return this;
        }

        #region Where

        public AVIMConversationQuery WhereContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            condition.WhereContainedIn(key, values);
            return this;
        }

        public AVIMConversationQuery WhereContainsAll<TIn>(string key, IEnumerable<TIn> values) {
            condition.WhereContainsAll(key, values);
            return this;
        }

        public AVIMConversationQuery WhereContains(string key, string substring) {
            condition.WhereContains(key, substring);
            return this;
        }

        public AVIMConversationQuery WhereDoesNotExist(string key) {
            condition.WhereDoesNotExist(key);
            return this;
        }

        public AVIMConversationQuery WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereDoesNotMatchQuery(key, query);
            return this;
        }

        public AVIMConversationQuery WhereEndsWith(string key, string suffix) {
            condition.WhereEndsWith(key, suffix);
            return this;
        }

        public AVIMConversationQuery WhereEqualTo(string key, object value) {
            condition.WhereEqualTo(key, value);
            return this;
        }

        public AVIMConversationQuery WhereSizeEqualTo(string key, uint size) {
            condition.WhereSizeEqualTo(key, size);
            return this;
        }

        public AVIMConversationQuery WhereExists(string key) {
            condition.WhereExists(key);
            return this;
        }

        public AVIMConversationQuery WhereGreaterThan(string key, object value) {
            condition.WhereGreaterThan(key, value);
            return this;
        }

        public AVIMConversationQuery WhereGreaterThanOrEqualTo(string key, object value) {
            condition.WhereGreaterThanOrEqualTo(key, value);
            return this;
        }

        public AVIMConversationQuery WhereLessThan(string key, object value) {
            condition.WhereLessThan(key, value);
            return this;
        }

        public AVIMConversationQuery WhereLessThanOrEqualTo(string key, object value) {
            condition.WhereLessThanOrEqualTo(key, value);
            return this;
        }

        public AVIMConversationQuery WhereMatches(string key, Regex regex, string modifiers) {
            condition.WhereMatches(key, regex, modifiers);
            return this;
        }

        public AVIMConversationQuery WhereMatches(string key, Regex regex) {
            return WhereMatches(key, regex, null);
        }

        public AVIMConversationQuery WhereMatches(string key, string pattern, string modifiers) {
            return WhereMatches(key, new Regex(pattern, RegexOptions.ECMAScript), modifiers);
        }

        public AVIMConversationQuery WhereMatches(string key, string pattern) {
            return WhereMatches(key, pattern, null);
        }

        public AVIMConversationQuery WhereMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereMatchesKeyInQuery(key, keyInQuery, query);
            return this;
        }

        public AVIMConversationQuery WhereDoesNotMatchesKeyInQuery<TOther>(string key, string keyInQuery, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereDoesNotMatchesKeyInQuery(key, keyInQuery, query);
            return this;
        }

        public AVIMConversationQuery WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query) where TOther : AVObject {
            condition.WhereMatchesQuery(key, query);
            return this;
        }

        public AVIMConversationQuery WhereNear(string key, AVGeoPoint point) {
            condition.WhereNear(key, point);
            return this;
        }

        public AVIMConversationQuery WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values) {
            condition.WhereNotContainedIn(key, values);
            return this;
        }

        public AVIMConversationQuery WhereNotEqualTo(string key, object value) {
            condition.WhereNotEqualTo(key, value);
            return this;
        }

        public AVIMConversationQuery WhereStartsWith(string key, string suffix) {
            condition.WhereStartsWith(key, suffix);
            return this;
        }

        public AVIMConversationQuery WhereWithinGeoBox(string key, AVGeoPoint southwest, AVGeoPoint northeast) {
            condition.WhereWithinGeoBox(key, southwest, northeast);
            return this;
        }

        public AVIMConversationQuery WhereWithinDistance(string key, AVGeoPoint point, AVGeoDistance maxDistance) {
            condition.WhereWithinDistance(key, point, maxDistance);
            return this;
        }

        public AVIMConversationQuery WhereRelatedTo(AVObject parent, string key) {
            condition.WhereRelatedTo(parent, key);
            return this;
        }

        #endregion

        public IDictionary<string, object> BuildParameters(string className = null) {
            return condition.BuildParameters(className);
        }

        public IDictionary<string, object> BuildWhere() {
            return condition.ToJSON();
        }
    }
}
