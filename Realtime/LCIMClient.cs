using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Realtime.Internal.Controller;

namespace LeanCloud.Realtime {
    public class LCIMClient {
        internal Dictionary<string, LCIMConversation> ConversationDict;

        public string Id {
            get; private set;
        }

        #region 事件

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
        public Action<int, string, string> OnClose {
            get; set;
        }

        /// <summary>
        /// 客户端连接断开
        /// </summary>
        public Action OnDisconnect {
            get; set;
        }

        /// <summary>
        /// 用户在其他客户端登录，当前客户端被服务端强行下线
        /// </summary>
        public Action<string> OnConflict {
            get; set;
        }

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

        /// <summary>
        /// 当前用户收到消息
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessage {
            get; set;
        }

        /// <summary>
        /// 消息被撤回
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageRecalled {
            get; set;
        }

        /// <summary>
        /// 消息被修改
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageUpdated {
            get; set;
        }

        /// <summary>
        /// 未读消息数目更新
        /// </summary>
        public Action<ReadOnlyCollection<LCIMConversation>> OnUnreadMessagesCountUpdated {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Action OnLastDeliveredAtUpdated {
            get; set;
        }

        public Action OnLastReadAtUpdated {
            get; set;
        }

        #endregion

        internal ILCIMSignatureFactory SignatureFactory {
            get; private set;
        }

        internal LCWebSocketConnection Connection {
            get; set;
        }

        internal LCIMSessionController SessionController {
            get; private set;
        }

        internal LCIMMessageController MessageController {
            get; private set;
        }

        internal LCIMUnreadController UnreadController {
            get; private set;
        }

        internal LCIMGoAwayController GoAwayController {
            get; private set;
        }

        internal LCIMConversationController ConversationController {
            get; private set;
        }

        public LCIMClient(string clientId,
            ILCIMSignatureFactory signatureFactory = null) {
            Id = clientId;
            SignatureFactory = signatureFactory;
            ConversationDict = new Dictionary<string, LCIMConversation>();

            SessionController = new LCIMSessionController(this);
            ConversationController = new LCIMConversationController(this);
            MessageController = new LCIMMessageController(this);
            UnreadController = new LCIMUnreadController(this);
            GoAwayController = new LCIMGoAwayController(this);

            Connection = new LCWebSocketConnection(Id) {
                OnNotification = OnNotification
            };
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public async Task Open() {
            await Connection.Connect();
            // 打开 Session
            await SessionController.Open();
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public async Task Close() {
            // 关闭 session
            await SessionController.Close();
            await Connection.Close();
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

        private async Task OnNotification(GenericCommand notification) {
            switch (notification.Cmd) {
                case CommandType.Session:
                    await SessionController.OnNotification(notification);
                    break;
                case CommandType.Conv:
                    await ConversationController.OnNotification(notification);
                    break;
                case CommandType.Direct:
                    await MessageController.OnNotification(notification);
                    break;
                case CommandType.Unread:
                    await UnreadController.OnNotification(notification);
                    break;
                case CommandType.Goaway:
                    await GoAwayController.OnNotification(notification);
                    break;
                default:
                    break;
            }
        }

        internal async Task<LCIMConversation> GetOrQueryConversation(string convId) {
            if (ConversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                return conversation;
            }
            conversation = await GetConversation(convId);
            return conversation;
        }

        internal GenericCommand NewCommand(CommandType cmd, OpType op) {
            GenericCommand command = NewCommand(cmd);
            command.Op = op;
            return command;
        }

        internal GenericCommand NewCommand(CommandType cmd) {
            return new GenericCommand {
                Cmd = cmd,
                AppId = LCApplication.AppId,
                PeerId = Id,
            };
        }

        internal GenericCommand NewDirectCommand() {
            return new GenericCommand {
                Cmd = CommandType.Direct,
                AppId = LCApplication.AppId,
                PeerId = Id,
            };
        }
    }
}
