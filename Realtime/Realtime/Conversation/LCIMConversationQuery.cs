using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using LeanCloud.Storage.Internal.Query;
using System.Linq;
using System.Collections.Generic;
using System;

namespace LeanCloud.Realtime {
    public class LCIMConversationQuery {
        internal const int CompactFlag = 0x1;
        internal const int WithLastMessageFlag = 0x2;

        internal LCCompositionalCondition Condition {
            get; private set;
        }

        private readonly LCIMClient client;

        /// <summary>
        /// Ignore the members of conversation.
        /// </summary>
        public bool Compact {
            get; set;
        } = false;

        /// <summary>
        /// With the last message.
        /// </summary>
        public bool WithLastMessageRefreshed {
            get; set;
        } = false;

        public LCIMConversationQuery(LCIMClient client) {
            Condition = new LCCompositionalCondition();
            this.client = client;
        }

        /// <summary>
        /// The value corresponding to key is equal to value, or the array corresponding to key contains value.
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
        /// The value corresponding to key is not equal to value, or the array corresponding to key does not contain value.
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
        /// Values contains value corresponding to key, or values contains at least one element in the array corresponding to key.
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
        /// The value of key must not be contained in values.
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
        /// The array corresponding to key contains all elements in values.
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
        /// The attribute corresponding to key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereExists(string key) {
            Condition.WhereExists(key);
            return this;
        }

        /// <summary>
        /// The attribute corresponding to key does not exist. 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereDoesNotExist(string key) {
            Condition.WhereDoesNotExist(key);
            return this;
        }

        /// <summary>
        /// The size of the array corresponding to key is equal to size.
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
        /// The value corresponding to key is greater than value.
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
        /// The value corresponding to key is greater than or equal to value.
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
        /// The value corresponding to key is less than value.
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
        /// The value corresponding to key is less than or equal to value.
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
        /// The string corresponding to key has a prefix.
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
        /// The string corresponding to key has a suffix.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereEndsWith(string key, string suffix) {
            Condition.WhereEndsWith(key, suffix);
            return this;
        }

        /// <summary>
        /// The string corresponding to key has a subString.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subString"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContains(string key, string subString) {
            Condition.WhereContains(key, subString);
            return this;
        }

        /// <summary>
        /// The ascending order by the value corresponding to key. 
        /// </summary>
        /// <param name="key">Multi-field sorting is supported with comma.</param>
        /// <returns></returns>
        public LCIMConversationQuery OrderBy(string key) {
            Condition.OrderByAscending(key);
            return this;
        }

        /// <summary>
        /// The descending order by the value corresponding to key.
        /// </summary>
        /// <param name="key">Multi-field sorting is supported with comma.</param>
        /// <returns></returns>
        public LCIMConversationQuery OrderByDescending(string key) {
            Condition.OrderByDescending(key);
            return this;
        }

        /// <summary>
        /// Includes nested LCObject for the provided key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery Include(string key) {
            Condition.Include(key);
            return this;
        }

        /// <summary>
        /// Restricts the keys of the LCObject returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery Select(string key) {
            Condition.Select(key);
            return this;
        }

        /// <summary>
        /// Sets the amount of results to skip before returning any results.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery Skip(int value) {
            Condition.Skip = value;
            return this;
        }

        /// <summary>
        /// Sets the limit of the number of results to return.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery Limit(int value) {
            Condition.Limit = value;
            return this;
        }

        /// <summary>
        /// Retrieves a list of LCObjects matching this query.
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMConversation>> Find() {
            return await client.ConversationController.Find(this);
        }

        /// <summary>
        /// Retrieves the first conversation from the query.
        /// </summary>
        /// <returns></returns>
        public async Task<LCIMConversation> First() {
            Limit(1);
            ReadOnlyCollection<LCIMConversation> conversations = await Find();
            return conversations?.First();
        }

        /// <summary>
        /// Retrieves the conversation 
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        public Task<LCIMConversation> Get(string convId) {
            if (string.IsNullOrEmpty(convId)) {
                throw new ArgumentNullException(nameof(convId));
            }
            WhereEqualTo("objectId", convId);
            return First();
        }
    }
}
