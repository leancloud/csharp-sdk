using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Storage.Internal;
using Newtonsoft.Json;

namespace LeanCloud.Realtime {
    public class LCIMClient {
        internal LCWebSocketConnection connection;

        internal Dictionary<string, LCIMConversation> conversationDict;

        public string ClientId {
            get; private set;
        }

        // TODO 判断过期
        internal string SessionToken {
            get; private set;
        }

        /// <summary>
        /// 当前用户被加入某个对话的黑名单
        /// </summary>
        public Action OnBlocked {
            get; set;
        }

        /// <summary>
        /// 当前客户端在某个对话中被禁言
        /// </summary>
        public Action<LCIMConversation, string> OnMuted;

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
        public Action OnOffline {
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
        public Action<LCIMConversation, Dictionary<string, object>, string> OnConversationInfoUpdated;

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
        public Action<LCIMConversation, List<string>, string> OnMembersJoined {
            get; set;
        }

        /// <summary>
        /// 有成员被从某个对话中移除
        /// </summary>
        public Action<LCIMConversation, List<string>, string> OnMembersLeft {
            get; set;
        }

        /// <summary>
        /// 有成员被加入某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, List<string>, string> OnMembersBlocked {
            get; set;
        }

        /// <summary>
        /// 有成员被移出某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, List<string>, string> OnMembersUnblocked {
            get; set;
        }

        /// <summary>
        /// 有成员在某个对话中被禁言
        /// </summary>
        public Action<LCIMConversation, List<string>, string> OnMembersMuted {
            get; set;
        }

        /// <summary>
        /// 有成员被移出某个对话的黑名单
        /// </summary>
        public Action<LCIMConversation, List<string>, string> OnMembersUnmuted {
            get; set;
        }

        /// <summary>
        /// 有成员的对话信息被更新
        /// </summary>
        public Action<LCIMConversation, string, Dictionary<string, object>, string> OnMemberInfoUpdated;

        /// <summary>
        /// 当前用户收到消息
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageReceived {
            get; set;
        }

        /// <summary>
        /// 消息被撤回
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageRecall {
            get; set;
        }

        /// <summary>
        /// 消息被修改
        /// </summary>
        public Action<LCIMConversation, LCIMMessage> OnMessageUpdate {
            get; set;
        }

        /// <summary>
        /// 未读消息数目更新
        /// </summary>
        public Action<List<LCIMConversation>> OnUnreadMessagesCountUpdated {
            get; set;
        }

        internal ILCIMSignatureFactory SignatureFactory {
            get; private set;
        }

        public LCIMClient(string clientId, ILCIMSignatureFactory signatureFactory = null) {
            ClientId = clientId;
            SignatureFactory = signatureFactory;
            conversationDict = new Dictionary<string, LCIMConversation>();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public async Task Open() {
            connection = new LCWebSocketConnection(ClientId) {
                OnNotification = OnNotification
            };
            await connection.Connect();
            // Open Session
            GenericCommand request = NewCommand(CommandType.Session, OpType.Open);
            SessionCommand session = new SessionCommand();
            if (SignatureFactory != null) {
                LCIMSignature signature = SignatureFactory.CreateConnectSignature(ClientId);
                session.S = signature.Signature;
                session.T = signature.Timestamp;
                session.N = signature.Nonce;
            }
            request.SessionMessage = session;
            GenericCommand response = await connection.SendRequest(request);
            SessionToken = response.SessionMessage.St;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public async Task Close() {
            GenericCommand request = NewCommand(CommandType.Session, OpType.Close);
            await connection.SendRequest(request);
            await connection.Close();
        }

        public async Task<LCIMChatRoom> CreateChatRoom(
            string name,
            Dictionary<string, object> properties = null) {
            LCIMChatRoom chatRoom = await CreateConv(name: name, transient: true, properties: properties) as LCIMChatRoom;
            return chatRoom;
        }

        public async Task<LCIMConversation> CreateConversation(
            IEnumerable<string> members,
            string name = null,
            bool unique = true,
            Dictionary<string, object> properties = null) {
            return await CreateConv(members: members, name: name, unique: unique, properties: properties);
        }

        public async Task<LCIMTemporaryConversation> CreateTemporaryConversation(
            IEnumerable<string> members,
            int ttl = 86400,
            Dictionary<string, object> properties = null) {
            LCIMTemporaryConversation tempConversation = await CreateConv(members: members, temporary: true, temporaryTtl: ttl, properties: properties) as LCIMTemporaryConversation;
            return tempConversation;
        }

        private async Task<LCIMConversation> CreateConv(
            IEnumerable<string> members = null,
            string name = null,
            bool transient = false,
            bool unique = true,
            bool temporary = false,
            int temporaryTtl = 86400,
            Dictionary<string, object> properties = null) {
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Start);
            ConvCommand conv = new ConvCommand {
                Transient = transient,
                Unique = unique,
            };
            if (members != null) {
                conv.M.AddRange(members);
            }
            if (!string.IsNullOrEmpty(name)) {
                conv.N = name;
            }
            if (temporary) {
                conv.TempConv = temporary;
                conv.TempConvTTL = temporaryTtl;
            }
            if (properties != null) {
                conv.Attr = new JsonObjectMessage {
                    Data = JsonConvert.SerializeObject(LCEncoder.Encode(properties))
                };
            }
            if (SignatureFactory != null) {
                LCIMSignature signature = SignatureFactory.CreateStartConversationSignature(ClientId, members);
                conv.S = signature.Signature;
                conv.T = signature.Timestamp;
                conv.N = signature.Nonce;
            }
            request.ConvMessage = conv;
            GenericCommand response = await connection.SendRequest(request);
            string convId = response.ConvMessage.Cid;
            if (!conversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                if (transient) {
                    conversation = new LCIMChatRoom(this);
                } else if (temporary) {
                    conversation = new LCIMTemporaryConversation(this);
                } else if (properties != null && properties.ContainsKey("system")) {
                    conversation = new LCIMServiceConversation(this);
                } else {
                    conversation = new LCIMConversation(this);
                }
                conversationDict[convId] = conversation;
            }
            // 合并请求数据
            conversation.Name = name;
            conversation.MemberIdList = members?.ToList();
            // 合并服务端推送的数据
            conversation.MergeFrom(response.ConvMessage);
            return conversation;
        }

        /// <summary>
        /// 获取某个特定的对话
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<LCIMConversation> GetConversation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            LCIMConversationQuery query = GetQuery()
                .WhereEqualTo("objectId", id)
                .Limit(1);
            List<LCIMConversation> results = await query.Find();
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
        public async Task<List<LCIMConversation>> GetConversationList(IEnumerable<string> ids) {
            if (ids == null || ids.Count() == 0) {
                throw new ArgumentNullException(nameof(ids));
            }
            List<LCIMConversation> conversationList = new List<LCIMConversation>();
            foreach (string id in ids) {
                LCIMConversation conversation = await GetConversation(id);
                conversationList.Add(conversation);
            }
            return conversationList;
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
                    await OnSessionNotification(notification);
                    break;
                case CommandType.Conv:
                    OnConversationNotification(notification);
                    break;
                case CommandType.Direct:
                    await OnDirectNotification(notification.DirectMessage);
                    break;
                case CommandType.Unread:
                    await OnUnreadNotification(notification.UnreadMessage);
                    break;
                default:
                    break;
            }
        }

        private async Task OnSessionNotification(GenericCommand notification) {
            switch (notification.Op) {
                case OpType.Closed:
                    await OnSessionClosed(notification.SessionMessage);
                    break;
                default:
                    break;
            }
        }

        private async Task OnSessionClosed(SessionCommand session) {
            int code = session.Code;
            string reason = session.Reason;
            string detail = session.Detail;
            await connection.Close();
            // TODO 关闭连接后回调给开发者

        }

        private void OnConversationNotification(GenericCommand notification) {
            ConvCommand conv = notification.ConvMessage;
            switch (notification.Op) {
                case OpType.Joined:
                    OnConversationJoined(conv);
                    break;
                case OpType.MembersJoined:
                    OnConversationMembersJoined(conv);
                    break;
                case OpType.Left:
                    OnConversationLeft(conv);
                    break;
                case OpType.MembersLeft:
                    OnConversationMemberLeft(conv);
                    break;
                case OpType.Updated:
                    OnConversationPropertiesUpdated(conv);
                    break;
                case OpType.MemberInfoChanged:
                    OnConversationMemberInfoChanged(conv);
                    break;
                default:
                    break;
            }
        }

        private async void OnConversationJoined(ConvCommand conv) {
            LCIMConversation conversation = await GetOrQueryConversation(conv.Cid);
            conversation.MergeFrom(conv);
            OnInvited?.Invoke(conversation, conv.InitBy);
        }

        private async void OnConversationMembersJoined(ConvCommand conv) {
            LCIMConversation conversation = await GetOrQueryConversation(conv.Cid);
            conversation.MergeFrom(conv);
            OnMembersJoined?.Invoke(conversation, conv.M.ToList(), conv.InitBy);
        }

        private void OnConversationLeft(ConvCommand conv) {
            if (conversationDict.TryGetValue(conv.Cid, out LCIMConversation conversation)) {
                OnKicked?.Invoke(conversation, conv.InitBy);
            }
        }

        private void OnConversationMemberLeft(ConvCommand conv) {
            if (conversationDict.TryGetValue(conv.Cid, out LCIMConversation conversation)) {
                List<string> leftIdList = conv.M.ToList();
                OnMembersLeft?.Invoke(conversation, leftIdList, conv.InitBy);
            }
        }

        private void OnConversationPropertiesUpdated(ConvCommand conv) {
            if (conversationDict.TryGetValue(conv.Cid, out LCIMConversation conversation)) {
                // TODO 修改对话属性，并回调给开发者

                OnConversationInfoUpdated?.Invoke(conversation, null, conv.InitBy);
            }
        }

        private void OnConversationMemberInfoChanged(ConvCommand conv) {

        }

        private async Task OnDirectNotification(DirectCommand direct) {
            LCIMMessage message = null;
            if (direct.HasBinaryMsg) {
                // 二进制消息
                byte[] bytes = direct.BinaryMsg.ToByteArray();
                message = new LCIMBinaryMessage(bytes);
            } else {
                // 文本消息
                string messageData = direct.Msg;
                Dictionary<string, object> msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(messageData,
                    new LCJsonConverter());
                int msgType = (int)(long)msg["_lctype"];
                switch (msgType) {
                    case -1:
                        message = new LCIMTextMessage();
                        break;
                    case -2:
                        message = new LCIMImageMessage();
                        break;
                    case -3:
                        message = new LCIMAudioMessage();
                        break;
                    case -4:
                        message = new LCIMVideoMessage();
                        break;
                    case -5:
                        message = new LCIMLocationMessage();
                        break;
                    case -6:
                        message = new LCIMFileMessage();
                        break;
                    default:
                        break;
                }
                message.Decode(direct);
            }
            // 获取对话
            LCIMConversation conversation = await GetOrQueryConversation(direct.Cid);
            OnMessageReceived?.Invoke(conversation, message);
        }

        private async Task OnUnreadNotification(UnreadCommand unread) {
            List<LCIMConversation> conversationList = new List<LCIMConversation>();
            foreach (UnreadTuple conv in unread.Convs) {
                // 查询对话
                LCIMConversation conversation = await GetOrQueryConversation(conv.Cid);
                conversation.Unread = conv.Unread;
                // TODO 反序列化对话
                // 最后一条消息
                JsonConvert.DeserializeObject<Dictionary<string, object>>(conv.Data);
                conversationList.Add(conversation);
            }
            OnUnreadMessagesCountUpdated?.Invoke(conversationList);
        }

        internal async Task<LCIMConversation> GetOrQueryConversation(string convId) {
            if (conversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                return conversation;
            }
            conversation = await GetConversation(convId);
            return conversation;
        }

        internal GenericCommand NewCommand(CommandType cmd, OpType op) {
            return new GenericCommand {
                Cmd = cmd,
                Op = op,
                AppId = LCApplication.AppId,
                PeerId = ClientId,
            };
        }

        internal GenericCommand NewDirectCommand() {
            return new GenericCommand {
                Cmd = CommandType.Direct,
                AppId = LCApplication.AppId,
                PeerId = ClientId,
            };
        }
    }
}
