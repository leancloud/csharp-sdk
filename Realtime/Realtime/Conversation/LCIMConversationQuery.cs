using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using LeanCloud.Storage.Internal.Query;

namespace LeanCloud.Realtime {
    public class LCIMConversationQuery {
        internal LCCompositionalCondition Condition {
            get; private set;
        }

        private readonly LCIMClient client;

        public LCIMConversationQuery(LCIMClient client) {
            Condition = new LCCompositionalCondition();
            this.client = client;
        }

        /// <summary>
        /// 等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereEqualTo(string key,
            object value) {
            Condition.WhereEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 不等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereNotEqualTo(string key,
            object value) {
            Condition.WhereNotEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContainedIn(string key,
            IEnumerable values) {
            Condition.WhereContainedIn(key, values);
            return this;
        }

        /// <summary>
        /// 不包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereNotContainedIn(string key,
            IEnumerable values) {
            Condition.WhereNotContainedIn(key, values);
            return this;
        }

        /// <summary>
        /// 包含全部
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContainsAll(string key,
            IEnumerable values) {
            Condition.WhereContainsAll(key, values);
            return this;
        }

        /// <summary>
        /// 存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereExists(string key) {
            Condition.WhereExists(key);
            return this;
        }

        /// <summary>
        /// 不存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereDoesNotExist(string key) {
            Condition.WhereDoesNotExist(key);
            return this;
        }

        /// <summary>
        /// 长度等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereSizeEqualTo(string key,
            int size) {
            Condition.WhereSizeEqualTo(key, size);
            return this;
        }

        /// <summary>
        /// 大于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereGreaterThan(string key,
            object value) {
            Condition.WhereGreaterThan(key, value);
            return this;
        }

        /// <summary>
        /// 大于等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereGreaterThanOrEqualTo(string key,
            object value) {
            Condition.WhereGreaterThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 小于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereLessThan(string key,
            object value) {
            Condition.WhereLessThan(key, value);
            return this;
        }

        /// <summary>
        /// 小于等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereLessThanOrEqualTo(string key,
            object value) {
            Condition.WhereLessThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 前缀
        /// </summary>
        /// <param name="key"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereStartsWith(string key,
            string prefix) {
            Condition.WhereStartsWith(key, prefix);
            return this;
        }

        /// <summary>
        /// 后缀
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereEndsWith(string key, string suffix) {
            Condition.WhereEndsWith(key, suffix);
            return this;
        }

        /// <summary>
        /// 字符串包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subString"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContains(string key, string subString) {
            Condition.WhereContains(key, subString);
            return this;
        }

        /// <summary>
        /// 按 key 升序
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery OrderBy(string key) {
            Condition.OrderByAscending(key);
            return this;
        }

        /// <summary>
        /// 按 key 降序
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery OrderByDescending(string key) {
            Condition.OrderByDescending(key);
            return this;
        }

        /// <summary>
        /// 拉取 key 的完整对象
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery Include(string key) {
            Condition.Include(key);
            return this;
        }

        /// <summary>
        /// 包含 key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery Select(string key) {
            Condition.Select(key);
            return this;
        }

        /// <summary>
        /// 跳过
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery Skip(int value) {
            Condition.Skip = value;
            return this;
        }

        /// <summary>
        /// 限制数量
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery Limit(int value) {
            Condition.Limit = value;
            return this;
        }

        public bool WithLastMessageRefreshed {
            get; set;
        }

        /// <summary>
        /// 查找
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMConversation>> Find() {
            return await client.ConversationController.Find(this);
        }
    }
}
