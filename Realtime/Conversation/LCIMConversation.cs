using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using LeanCloud.Storage;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 普通对话
    /// </summary>
    public class LCIMConversation {
        /// <summary>
        /// 对话 Id
        /// </summary>
        public string Id {
            get; internal set;
        }

        /// <summary>
        /// 是否唯一
        /// </summary>
        public bool Unique {
            get; internal set;
        }

        /// <summary>
        /// 唯一 Id
        /// </summary>
        public string UniqueId {
            get; internal set;
        }

        /// <summary>
        /// 对话名称
        /// </summary>
        public string Name {
            get {
                return this["name"] as string;
            }
            internal set {
                this["name"] = value;
            }
        }

        /// <summary>
        /// 创建者 Id
        /// </summary>
        public string CreatorId {
            get; set;
        }

        /// <summary>
        /// 成员 Id
        /// </summary>
        public ReadOnlyCollection<string> MemberIds {
            get {
                return new ReadOnlyCollection<string>(ids.ToList());
            }
        }

        /// <summary>
        /// 静音成员 Id
        /// </summary>
        public ReadOnlyCollection<string> MutedMemberIds {
            get {
                return new ReadOnlyCollection<string>(mutedIds.ToList());
            }
        }

        /// <summary>
        /// 未读消息数量
        /// </summary>
        public int Unread {
            get; internal set;
        }

        /// <summary>
        /// 最新的一条消息
        /// </summary>
        public LCIMMessage LastMessage {
            get; internal set;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt {
            get; internal set;
        }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt {
            get; internal set;
        }

        /// <summary>
        /// 最新送达消息时间戳
        /// </summary>
        public long LastDeliveredTimestamp {
            get; internal set;
        }

        /// <summary>
        /// 最新送达消息时间
        /// </summary>
        public DateTime LastDeliveredAt {
            get {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(LastDeliveredTimestamp);
                return dateTimeOffset.DateTime;
            }
        }

        /// <summary>
        /// 最新已读消息时间戳
        /// </summary>
        public long LastReadTimestamp {
            get; internal set;
        }

        /// <summary>
        /// 最新已读消息时间
        /// </summary>
        public DateTime LastReadAt {
            get {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(LastReadTimestamp);
                return dateTimeOffset.DateTime;
            }
        }

        /// <summary>
        /// 设置/获取对话属性
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key] {
            get {
                return customProperties[key];
            }
            set {
                customProperties[key] = value;
            }
        }

        /// <summary>
        /// 是否已静音
        /// </summary>
        public bool IsMute {
            get; private set;
        }

        protected LCIMClient Client {
            get; private set;
        }

        private readonly Dictionary<string, object> customProperties;

        internal HashSet<string> ids;

        internal HashSet<string> mutedIds;

        internal LCIMConversation(LCIMClient client) {
            Client = client;
            customProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取对话人数，或暂态对话的在线人数
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetMembersCount() {
            return await Client.ConversationController.GetMembersCount(Id);
        }

        /// <summary>
        /// 将该会话标记为已读
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task Read() {
            if (LastMessage == null) {
                return;
            }
            await Client.MessageController.Read(Id, LastMessage);
        }

        /// <summary>
        /// 修改对话属性
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public async Task UpdateInfo(Dictionary<string, object> attributes) {
            if (attributes == null || attributes.Count == 0) {
                throw new ArgumentNullException(nameof(attributes));
            }
            Dictionary<string, object> updatedAttr = await Client.ConversationController.UpdateInfo(Id, attributes);
            if (updatedAttr != null) {
                MergeInfo(updatedAttr);
            }
        }

        /// <summary>
        /// 添加用户到对话
        /// </summary>
        /// <param name="clientIds">用户 Id</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> AddMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.AddMembers(Id, clientIds);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="removeIds">用户 Id</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> RemoveMembers(IEnumerable<string> removeIds) {
            if (removeIds == null || removeIds.Count() == 0) {
                throw new ArgumentNullException(nameof(removeIds));
            }
            return await Client.ConversationController.RemoveMembers(Id, removeIds);
        }

        /// <summary>
        /// 加入对话
        /// </summary>
        /// <returns></returns>
        public async Task Join() {
            LCIMPartiallySuccessResult result = await AddMembers(new string[] { Client.Id });
            if (!result.IsSuccess) {
                LCIMOperationFailure error = result.FailureList[0];
                throw new LCException(error.Code, error.Reason);
            }
        }

        /// <summary>
        /// 离开对话
        /// </summary>
        /// <returns></returns>
        public async Task Quit() {
            LCIMPartiallySuccessResult result = await RemoveMembers(new string[] { Client.Id });
            if (!result.IsSuccess) {
                LCIMOperationFailure error = result.FailureList[0];
                throw new LCException(error.Code, error.Reason);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<LCIMMessage> Send(LCIMMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            await Client.MessageController.Send(Id, message);
            return message;
        }

        /// <summary>
        /// 静音
        /// </summary>
        /// <returns></returns>
        public async Task Mute() {
            await Client.ConversationController.Mute(Id);
            IsMute = true;
        }

        /// <summary>
        /// 取消静音
        /// </summary>
        /// <returns></returns>
        public async Task Unmute() {
            await Client.ConversationController.Unmute(Id);
            IsMute = false;
        }

        /// <summary>
        /// 禁言
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> MuteMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.MuteMembers(Id, clientIds);
        }

        /// <summary>
        /// 取消禁言
        /// </summary>
        /// <param name="clientIdList"></param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> UnmuteMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.UnmuteMembers(Id, clientIds);
        }

        /// <summary>
        /// 将用户加入黑名单
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> BlockMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.BlockMembers(Id, clientIds);
        }

        /// <summary>
        /// 将用户移除黑名单
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> UnblockMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.UnblockMembers(Id, clientIds);
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task RecallMessage(LCIMMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            await Client.MessageController.RecallMessage(Id, message);
        }

        /// <summary>
        /// 修改消息
        /// </summary>
        /// <param name="oldMessage"></param>
        /// <param name="newMessage"></param>
        /// <returns></returns>
        public async Task UpdateMessage(LCIMMessage oldMessage, LCIMMessage newMessage) {
            if (oldMessage == null) {
                throw new ArgumentNullException(nameof(oldMessage));
            }
            if (newMessage == null) {
                throw new ArgumentNullException(nameof(newMessage));
            }
            await Client.MessageController.UpdateMessage(Id, oldMessage, newMessage);
        }

        /// <summary>
        /// 更新对话中成员的角色
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task UpdateMemberRole(string memberId, string role) {
            if (string.IsNullOrEmpty(memberId)) {
                throw new ArgumentNullException(nameof(memberId));
            }
            if (role != LCIMConversationMemberInfo.Manager && role != LCIMConversationMemberInfo.Member) {
                throw new ArgumentException("role MUST be Manager Or Memebr");
            }
            await Client.ConversationController.UpdateMemberRole(Id, memberId, role);
        }

        /// <summary>
        /// 获取对话中成员的角色（只返回管理员）
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMConversationMemberInfo>> GetAllMemberInfo() {
            return await Client.ConversationController.GetAllMemberInfo(Id);
        }

        /// <summary>
        /// 获取对话中指定成员的角色
        /// </summary>
        /// <param name="memberId"></param>
        /// <returns></returns>
        public async Task<LCIMConversationMemberInfo> GetMemberInfo(string memberId) {
            if (string.IsNullOrEmpty(memberId)) {
                throw new ArgumentNullException(nameof(memberId));
            }
            ReadOnlyCollection<LCIMConversationMemberInfo> members = await GetAllMemberInfo();
            foreach (LCIMConversationMemberInfo member in members) {
                if (member.MemberId == memberId) {
                    return member;
                }
            }
            return null;
        }

        /// <summary>
        /// 查询禁言用户
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task<LCIMPageResult> QueryMutedMembers(int limit = 10,
            string next = null) {
            return await Client.ConversationController.QueryMutedMembers(Id, limit, next);
        }

        /// <summary>
        /// 查询黑名单用户
        /// </summary>
        /// <param name="limit">限制</param>
        /// <param name="next">其实用户 Id</param>
        /// <returns></returns>
        public async Task<LCIMPageResult> QueryBlockedMembers(int limit = 10,
            string next = null) {
            return await Client.ConversationController.QueryBlockedMembers(Id, limit, next);
        }

        /// <summary>
        /// 查询聊天记录
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="direction">查找方向</param>
        /// <param name="limit">限制</param>
        /// <param name="messageType">消息类型</param>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMMessage>> QueryMessages(LCIMMessageQueryEndpoint start = null,
            LCIMMessageQueryEndpoint end = null,
            LCIMMessageQueryDirection direction = LCIMMessageQueryDirection.NewToOld,
            int limit = 20,
            int messageType = 0) {
            return await Client.MessageController.QueryMessages(Id, start, end, direction, limit, messageType);
        }

        /// <summary>
        /// 获取会话已收/已读时间戳
        /// </summary>
        /// <returns></returns>
        public async Task FetchReciptTimestamps() {
            await Client.ConversationController.FetchReciptTimestamp(Id);
        }

        internal static bool IsTemporayConversation(string convId) {
            return convId.StartsWith("_tmp:");
        }

        internal void MergeFrom(Dictionary<string, object> conv) {
            if (conv.TryGetValue("objectId", out object idObj)) {
                Id = idObj as string;
            }
            if (conv.TryGetValue("unique", out object uniqueObj)) {
                Unique = (bool)uniqueObj;
            }
            if (conv.TryGetValue("uniqueId", out object uniqueIdObj)) {
                UniqueId = uniqueIdObj as string;
            }
            if (conv.TryGetValue("createdAt", out object createdAtObj)) {
                CreatedAt = DateTime.Parse(createdAtObj.ToString());
            }
            if (conv.TryGetValue("updatedAt", out object updatedAtObj)) {
                UpdatedAt = DateTime.Parse(updatedAtObj.ToString());
            }
            if (conv.TryGetValue("c", out object co)) {
                CreatorId = co as string;
            }
            if (conv.TryGetValue("m", out object mo)) {
                IEnumerable<string> ids = (mo as IList<object>).Cast<string>();
                this.ids = new HashSet<string>(ids);
            }
            if (conv.TryGetValue("mu", out object muo)) {
                IEnumerable<string> ids = (muo as IList<object>).Cast<string>();
                mutedIds = new HashSet<string>(ids);
            }
            //if (conv.TryGetValue("lm", out object lmo)) {
            //    LastMessageAt = (DateTime)LCDecoder.Decode(lmo);
            //}
        }

        internal void MergeInfo(Dictionary<string, object> attr) {
            if (attr == null || attr.Count == 0) {
                return;
            }
            foreach (KeyValuePair<string, object> kv in attr) {
                customProperties[kv.Key] = kv.Value;
            }
        }
    }
}
