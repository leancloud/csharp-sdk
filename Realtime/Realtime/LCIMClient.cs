using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using LeanCloud.Storage;
using LeanCloud.Realtime.Internal.Protocol;
using LeanCloud.Realtime.Internal.Controller;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 通信客户端
    /// </summary>
    public class LCIMClient {
        /// <summary>
        /// 对话缓存
        /// </summary>
        internal Dictionary<string, LCIMConversation> ConversationDict;

        /// <summary>
        /// 用户 Id
        /// </summary>
        public string Id {
            get; private set;
        }

        /// <summary>
        /// 用户标识
        /// </summary>
        public string Tag {
            get; private set;
        }

        /// <summary>
        /// 设备 Id
        /// </summary>
        public string DeviceId {
            get; private set;
        }

        /// <summary>
        /// 登录 tokens
        /// </summary>
        internal string SessionToken {
            get; private set;
        }

        #region 事件

        #region 连接状态事件

        /// <summary>
        /// 客户端连接断开
        /// </summary>
        public Action OnPaused {
            get; set;
        }

        /// <summary>
        /// 客户端连接恢复正常
        /// </summary>
        public Action OnResume {
            get; set;
        }

        /// <summary>
        /// 当前客户端被服务端强行下线
        /// </summary>
        public Action<int, string> OnClose {
            get; set;
        }

        #endregion

        #region 对话事件

        /// <summary>
        /// 当前用户被加入某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, string> OnBlocked {
            get; set;
        }

        /// <summary>
        /// 当用户被解除黑名单
        /// </summary>
        public Action<LCIMConversation, string> OnUnblocked {
            get; set;
        }

        /// <summary>
        /// 当前用户在某个对话中被禁言
        /// </summary>
        public Action<LCIMConversation, string> OnMuted;

        /// <summary>
        /// 当前用户在某个对话中被解除禁言
        /// </summary>
        public Action<LCIMConversation, string> OnUnmuted;

        /// <summary>
        /// 该对话信息被更新
        /// </summary>
        public Action<LCIMConversation, ReadOnlyDictionary<string, object>, string> OnConversationInfoUpdated;

        /// <summary>
        /// 当前用户被添加至某个对话
        /// </summary>
        public Action<LCIMConversation, string> OnInvited {
            get; set;
        }

        /// <summary>
        /// 当前用户被从某个对话中移除
        /// </summary>
        public Action<LCIMConversation, string> OnKicked {
            get; set;
        }

        /// <summary>
        /// 有用户被添加至某个对话
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersJoined {
            get; set;
        }

        /// <summary>
        /// 有成员被从某个对话中移除
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersLeft {
            get; set;
        }

        /// <summary>
        /// 有成员被加入某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersBlocked {
            get; set;
        }

        /// <summary>
        /// 有成员被移出某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersUnblocked {
            get; set;
        }

        /// <summary>
        /// 有成员在某个对话中被禁言
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersMuted {
            get; set;
        }

        /// <summary>
        /// 有成员被移出某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersUnmuted {
            get; set;
        }

        /// <summary>
        /// 有成员的对话信息被更新
        /// </summary>
        public Action<LCIMConversation, string, string, string> OnMemberInfoUpdated;

        #endregion

        #region 消息事件

        /// <summary>
        /// 当前用户收到消息
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessage {
            get; set;
        }

        /// <summary>
        /// 消息被撤回
        /// </summary>
        public Action<LCIMConversation, LCIMRecalledMessage> OnMessageRecalled {
            get; set;
        }

        /// <summary>
        /// 消息被修改
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageUpdated {
            get; set;
        }

        /// <summary>
        /// 消息已送达
        /// </summary>
        public Action<LCIMConversation, string> OnMessageDelivered {
            get; set;
        }

        /// <summary>
        /// 消息已读
        /// </summary>
        public Action<LCIMConversation, string> OnMessageRead {
            get; set;
        }

        /// <summary>
        /// 未读消息数目更新
        /// </summary>
        public Action<ReadOnlyCollection<LCIMConversation>> OnUnreadMessagesCountUpdated {
            get; set;
        }

        /// <summary>
        /// 最近分发消息更新
        /// </summary>
        public Action OnLastDeliveredAtUpdated {
            get; set;
        }

        /// <summary>
        /// 最近已读消息更新
        /// </summary>
        public Action OnLastReadAtUpdated {
            get; set;
        }

        #endregion

        #endregion

        internal ILCIMSignatureFactory SignatureFactory {
            get; private set;
        }

        internal LCIMSessionController SessionController {
            get; private set;
        }

        internal LCIMMessageController MessageController {
            get; private set;
        }

        internal LCIMConversationController ConversationController {
            get; private set;
        }

        #region 接口

        public LCIMClient(string clientId,
            string tag = null,
            string deviceId = null,
            ILCIMSignatureFactory signatureFactory = null) {
            if (string.IsNullOrEmpty(clientId)) {
                throw new ArgumentNullException(nameof(clientId));
            }
            SetUpClient(clientId, tag, deviceId, signatureFactory);
        }

        public LCIMClient(LCUser user,
            string tag = null,
            string deviceId = null,
            ILCIMSignatureFactory signatureFactory = null) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrEmpty(user.ObjectId) ||
                string.IsNullOrEmpty(user.SessionToken)) {
                throw new ArgumentException("User must be authenticacted.");
            }
            SetUpClient(user.ObjectId, tag, deviceId, signatureFactory);
            SessionToken = user.SessionToken;
        }

        private void SetUpClient(string clientId,
            string tag,
            string deviceId,
            ILCIMSignatureFactory signatureFactory) {
            Id = clientId;
            Tag = tag;
            DeviceId = deviceId;
            SignatureFactory = signatureFactory;

            ConversationDict = new Dictionary<string, LCIMConversation>();

            // 模块
            SessionController = new LCIMSessionController(this);
            ConversationController = new LCIMConversationController(this);
            MessageController = new LCIMMessageController(this);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="force">是否强制登录</param>
        /// <returns></returns>
        public async Task Open(bool force = true) {
            try {
                // 打开 Session
                await SessionController.Open(force);
            } catch (Exception e) {
                LCLogger.Error(e);
                // 如果 session 阶段异常，则关闭连接
                throw e;
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public async Task Close() {
            // 关闭 session
            await SessionController.Close();
        }

        /// <summary>
        /// 创建普通对话
        /// </summary>
        /// <param name="members"></param>
        /// <param name="name"></param>
        /// <param name="unique"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task<LCIMConversation> CreateConversation(
            IEnumerable<string> members,
            string name = null,
            bool unique = true,
            Dictionary<string, object> properties = null) {
            return await ConversationController.CreateConv(members: members,
                name: name,
                unique: unique,
                properties: properties);
        }

        /// <summary>
        /// 创建聊天室
        /// </summary>
        /// <param name="name"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task<LCIMChatRoom> CreateChatRoom(
            string name,
            Dictionary<string, object> properties = null) {
            LCIMChatRoom chatRoom = await ConversationController.CreateConv(name: name,
                transient: true,
                properties: properties) as LCIMChatRoom;
            return chatRoom;
        }

        /// <summary>
        /// 创建临时对话
        /// </summary>
        /// <param name="members"></param>
        /// <param name="ttl"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public async Task<LCIMTemporaryConversation> CreateTemporaryConversation(
            IEnumerable<string> members,
            int ttl = 86400,
            Dictionary<string, object> properties = null) {
            LCIMTemporaryConversation tempConversation = await ConversationController.CreateConv(members: members,
                temporary: true,
                temporaryTtl: ttl,
                properties: properties) as LCIMTemporaryConversation;
            return tempConversation;
        }

        /// <summary>
        /// 根据 id 获取对话
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<LCIMConversation> GetConversation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            if (LCIMConversation.IsTemporayConversation(id)) {
                List<LCIMTemporaryConversation> temporaryConversationList = await ConversationController.GetTemporaryConversations(new string[] { id });
                if (temporaryConversationList == null || temporaryConversationList.Count < 1) {
                    return null;
                }
                return temporaryConversationList[0];
            }
            LCIMConversationQuery query = GetQuery()
                .WhereEqualTo("objectId", id)
                .Limit(1);
            ReadOnlyCollection<LCIMConversation> results = await ConversationController.Find(query);
            if (results == null || results.Count < 1) {
                return null;
            }
            return results[0];
        }

        /// <summary>
        /// 获取某些特定的对话
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMConversation>> GetConversationList(IEnumerable<string> ids) {
            if (ids == null || ids.Count() == 0) {
                throw new ArgumentNullException(nameof(ids));
            }
            // 区分临时对话
            IEnumerable<string> tempConvIds = ids.Where(item => {
                return LCIMConversation.IsTemporayConversation(item);
            });
            IEnumerable<string> convIds = ids.Where(item => {
                return !tempConvIds.Contains(item);
            });
            List<LCIMConversation> conversationList = new List<LCIMConversation>();
            if (tempConvIds.Count() > 0) {
                List<LCIMTemporaryConversation> temporaryConversations = await ConversationController.GetTemporaryConversations(tempConvIds);
                conversationList.AddRange(temporaryConversations);
            }
            if (convIds.Count() > 0) {
                LCIMConversationQuery query = GetQuery()
                    .WhereContainedIn("objectId", convIds)
                    .Limit(convIds.Count());
                ReadOnlyCollection<LCIMConversation> conversations = await ConversationController.Find(query);
                conversationList.AddRange(conversations);
            }
            return conversationList.AsReadOnly();
        }

        /// <summary>
        /// 获取对话查询对象
        /// </summary>
        /// <returns></returns>
        public LCIMConversationQuery GetQuery() {
            return new LCIMConversationQuery(this);
        }

        #endregion

        internal void HandleNotification(GenericCommand notification) {
            switch (notification.Cmd) {
                case CommandType.Session:
                    SessionController.HandleNotification(notification);
                    break;
                case CommandType.Conv:
                case CommandType.Unread:
                    ConversationController.HandleNotification(notification);
                    break;
                case CommandType.Direct:
                case CommandType.Patch:
                case CommandType.Rcp:
                    MessageController.HandleNotification(notification);
                    break;
                default:
                    break;
            }
        }

        internal void HandleDisconnected() {
            OnPaused?.Invoke();
        }

        internal async void HandleReconnected() {
            try {
                // 打开 Session
                await SessionController.Reopen();
                // 回调用户
                OnResume?.Invoke();
            } catch (Exception e) {
                LCLogger.Error(e);
                // 重连成功，但 session/open 失败
                OnClose?.Invoke(0, string.Empty);
            }
        }
        
        internal async Task<LCIMConversation> GetOrQueryConversation(string convId) {
            if (ConversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                return conversation;
            }
            conversation = await GetConversation(convId);
            return conversation;
        }
    }
}
