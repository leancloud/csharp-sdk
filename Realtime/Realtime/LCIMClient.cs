using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using LeanCloud.Storage;
using LeanCloud.Realtime.Internal.Protocol;
using LeanCloud.Realtime.Internal.Controller;

namespace LeanCloud.Realtime {

    public class LCIMClient {
        /// <summary>
        /// Conversation cache
        /// </summary>
        internal Dictionary<string, LCIMConversation> ConversationDict;

        /// <summary>
        /// Client Id
        /// </summary>
        public string Id {
            get; private set;
        }

        /// <summary>
        /// Client tag
        /// </summary>
        public string Tag {
            get; private set;
        }

        public string DeviceId {
            get; private set;
        }

        internal string SessionToken {
            get; private set;
        }

        #region 事件

        #region 连接状态事件

        /// <summary>
        /// Occurs when the connection is lost.
        /// </summary>
        public Action OnPaused {
            get; set;
        }

        /// <summary>
        /// Occurs when the connection is recovered. 
        /// </summary>
        public Action OnResume {
            get; set;
        }

        /// <summary>
        /// Occurs when the connection is closed and there will be no auto reconnection.
        /// Possible causes include there is a single device login conflict or the client has been kicked off by the server.
        /// </summary>
        public Action<int, string> OnClose {
            get; set;
        }

        #endregion

        #region 对话事件

        /// <summary>
        /// Occurs when the current user is added into the blacklist of a conversation.
        /// </summary>
        public Action<LCIMConversation, string> OnBlocked {
            get; set;
        }

        /// <summary>
        /// Occurs when the current user is removed from the blacklist of a conversation.
        /// </summary>
        public Action<LCIMConversation, string> OnUnblocked {
            get; set;
        }

        /// <summary>
        /// Occurs when the current user is muted in a conversation.
        /// </summary>
        public Action<LCIMConversation, string> OnMuted;

        /// <summary>
        /// Occurs when the current user is unmuted in a conversation. 
        /// </summary>
        public Action<LCIMConversation, string> OnUnmuted;

        /// <summary>
        /// Occurs when the properties of a conversation are updated.
        /// </summary>
        public Action<LCIMConversation, ReadOnlyDictionary<string, object>, string> OnConversationInfoUpdated;

        /// <summary>
        /// Occurs when the current user is invited to a conversation.
        /// </summary>
        public Action<LCIMConversation, string> OnInvited {
            get; set;
        }

        /// <summary>
        /// Occurs when the current user is kicked from a conversation.
        /// </summary>
        public Action<LCIMConversation, string> OnKicked {
            get; set;
        }

        /// <summary>
        /// Occurs when a user joined a conversation.
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersJoined {
            get; set;
        }

        /// <summary>
        /// Occurs when a user left a conversation. 
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersLeft {
            get; set;
        }

        /// <summary>
        /// Occurs when a user is added to the blacklist of a conversation.
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersBlocked {
            get; set;
        }

        /// <summary>
        /// Occurs when a user is removed from the blacklist of a conversation. 
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersUnblocked {
            get; set;
        }

        /// <summary>
        /// Occurs when a user is muted in a conversation. 
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersMuted {
            get; set;
        }

        /// <summary>
        /// Occurs when a user is unmuted in a conversation.
        /// </summary>
        public Action<LCIMConversation, ReadOnlyCollection<string>, string> OnMembersUnmuted {
            get; set;
        }

        /// <summary>
        /// Occurs when the properties of someone are updated.
        /// </summary>
        public Action<LCIMConversation, string, string, string> OnMemberInfoUpdated;

        #endregion

        #region 消息事件

        /// <summary>
        /// Occurs when a new message is delivered to a conversation the current user is already in.
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessage {
            get; set;
        }

        /// <summary>
        /// Occurs when a message is recalled.
        /// </summary>
        public Action<LCIMConversation, LCIMRecalledMessage> OnMessageRecalled {
            get; set;
        }

        /// <summary>
        /// Occurs when a message is updated.
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageUpdated {
            get; set;
        }

        /// <summary>
        /// Occurs when a message is delivered.
        /// </summary>
        public Action<LCIMConversation, string> OnMessageDelivered {
            get; set;
        }

        /// <summary>
        /// Occurs when a message is read.
        /// </summary>
        public Action<LCIMConversation, string> OnMessageRead {
            get; set;
        }

        /// <summary>
        /// Occurs when the number of unreadMessagesCount is updatded.
        /// </summary>
        public Action<ReadOnlyCollection<LCIMConversation>> OnUnreadMessagesCountUpdated {
            get; set;
        }

        /// <summary>
        /// Occurs when the last delivered message is updated.
        /// </summary>
        public Action OnLastDeliveredAtUpdated {
            get; set;
        }

        /// <summary>
        /// Occurs when the last delivered message is updated.
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
        /// Signing in
        /// </summary>
        /// <param name="force">If this is ture (default value), and single device sign-on is enabled, users already logged in on another device with the same tag will be logged out.</param>
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
        /// Closes the session
        /// </summary>
        /// <returns></returns>
        public async Task Close() {
            // 关闭 session
            await SessionController.Close();
        }

        /// <summary>
        /// Creates a conversation
        /// </summary>
        /// <param name="members">The list of clientIds of participants in this conversation (except the creator)</param>
        /// <param name="name">The name of this conversation</param>
        /// <param name="unique">Whether this conversation is unique;
        /// if it is true and an existing conversation contains the same composition of members,
        /// the existing conversation will be reused, otherwise a new conversation will be created.</param>
        /// <param name="properties">Custom attributes of this conversation</param>
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
        /// Creates a chatroom
        /// </summary>
        /// <param name="name">The name of this chatroom</param>
        /// <param name="properties">Custom attributes of this chatroom</param>
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
        /// Creates a temporary conversation
        /// </summary>
        /// <param name="members">The list of clientIds of participants in this temporary conversation (except the creator)</param>
        /// <param name="ttl">TTL of this temporary conversation</param>
        /// <param name="properties">Custom attributes of this temporary conversation</param>
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
        /// Queries a conversation based on its id.
        /// </summary>
        /// <param name="id">objectId</param>
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
        /// Queries conversations based on their ids.
        /// </summary>
        /// <param name="ids">objectId list</param>
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
        /// Constructs a conversation query.
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
