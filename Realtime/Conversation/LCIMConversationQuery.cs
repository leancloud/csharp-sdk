using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using LeanCloud.Storage.Internal.Query;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal;
using LeanCloud.Storage.Internal.Codec;
using Newtonsoft.Json;

namespace LeanCloud.Realtime {
    public class LCIMConversationQuery {
        private LCCompositionalCondition condition;

        private LCIMClient client;

        public LCIMConversationQuery(LCIMClient client) {
            condition = new LCCompositionalCondition();
            this.client = client;
        }

        /// <summary>
        /// 等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereEqualTo(string key, object value) {
            condition.WhereEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 不等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereNotEqualTo(string key, object value) {
            condition.WhereNotEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContainedIn(string key, IEnumerable values) {
            condition.WhereContainedIn(key, values);
            return this;
        }

        /// <summary>
        /// 包含全部
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContainsAll(string key, IEnumerable values) {
            condition.WhereContainsAll(key, values);
            return this;
        }

        /// <summary>
        /// 存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereExists(string key) {
            condition.WhereExists(key);
            return this;
        }

        /// <summary>
        /// 不存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereDoesNotExist(string key) {
            condition.WhereDoesNotExist(key);
            return this;
        }

        /// <summary>
        /// 长度等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereSizeEqualTo(string key, int size) {
            condition.WhereSizeEqualTo(key, size);
            return this;
        }

        /// <summary>
        /// 大于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereGreaterThan(string key, object value) {
            condition.WhereGreaterThan(key, value);
            return this;
        }

        /// <summary>
        /// 大于等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereGreaterThanOrEqualTo(string key, object value) {
            condition.WhereGreaterThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 小于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereLessThan(string key, object value) {
            condition.WhereLessThan(key, value);
            return this;
        }

        /// <summary>
        /// 小于等于
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereLessThanOrEqualTo(string key, object value) {
            condition.WhereLessThanOrEqualTo(key, value);
            return this;
        }

        /// <summary>
        /// 前缀
        /// </summary>
        /// <param name="key"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereStartsWith(string key, string prefix) {
            condition.WhereStartsWith(key, prefix);
            return this;
        }

        /// <summary>
        /// 后缀
        /// </summary>
        /// <param name="key"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereEndsWith(string key, string suffix) {
            condition.WhereEndsWith(key, suffix);
            return this;
        }

        /// <summary>
        /// 字符串包含
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subString"></param>
        /// <returns></returns>
        public LCIMConversationQuery WhereContains(string key, string subString) {
            condition.WhereContains(key, subString);
            return this;
        }

        /// <summary>
        /// 按 key 升序
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery OrderBy(string key) {
            condition.OrderBy(key);
            return this;
        }

        /// <summary>
        /// 按 key 降序
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery OrderByDescending(string key) {
            condition.OrderByDescending(key);
            return this;
        }

        /// <summary>
        /// 拉取 key 的完整对象
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery Include(string key) {
            condition.Include(key);
            return this;
        }

        /// <summary>
        /// 包含 key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LCIMConversationQuery Select(string key) {
            condition.Select(key);
            return this;
        }

        /// <summary>
        /// 跳过
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery Skip(int value) {
            condition.Skip = value;
            return this;
        }

        /// <summary>
        /// 限制数量
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public LCIMConversationQuery Limit(int value) {
            condition.Limit = value;
            return this;
        }

        public bool WithLastMessageRefreshed {
            get; set;
        }

        /// <summary>
        /// 查找
        /// </summary>
        /// <returns></returns>
        public async Task<List<LCIMConversation>> Find() {
            GenericCommand command = new GenericCommand {
                Cmd = CommandType.Conv,
                Op = OpType.Query,
                AppId = LCApplication.AppId,
                PeerId = client.Id,
            };
            ConvCommand conv = new ConvCommand();
            string where = condition.BuildWhere();
            if (!string.IsNullOrEmpty(where)) {
                conv.Where = new JsonObjectMessage {
                    Data = where
                };
            }
            command.ConvMessage = conv;
            GenericCommand response = await client.Connection.SendRequest(command);
            JsonObjectMessage results = response.ConvMessage.Results;
            List<object> convs = JsonConvert.DeserializeObject<List<object>>(results.Data, new LCJsonConverter());
            List<LCIMConversation> convList = new List<LCIMConversation>(convs.Count);
            foreach (object c in convs) {
                Dictionary<string, object> cd = c as Dictionary<string, object>;
                string convId = cd["objectId"] as string;
                if (!client.ConversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                    conversation = new LCIMConversation(client);
                    client.ConversationDict[convId] = conversation;
                }
                conversation.MergeFrom(cd);
                convList.Add(conversation);
            }
            return convList;
        }
    }
}
