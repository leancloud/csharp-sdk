using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Realtime.Internal.WebSocket;
using LeanCloud.Realtime.Protocol;
using Google.Protobuf;
using Newtonsoft.Json;

namespace LeanCloud.Realtime {
    public class LCIMClient {
        internal LCWebSocketConnection connection;

        private Dictionary<string, LCIMConversation> conversationDict;

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

        public Action<LCIMConversation, List<string>, string> OnMembersLeft {
            get; set;
        }

        public Action<LCIMConversation, LCIMMessage> OnMessageReceived {
            get; set;
        }

        public LCIMClient(string clientId) {
            ClientId = clientId;
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
            request.SessionMessage = new SessionCommand();
            GenericCommand response = await connection.SendRequest(request);
            SessionToken = response.SessionMessage.St;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public async Task Close() {
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
            GenericCommand command = NewCommand(CommandType.Conv, OpType.Start);
            ConvCommand conv = new ConvCommand {
                Transient = transient,
                Unique = unique,
                TempConv = temporary,
                TempConvTTL = temporaryTtl
            };
            if (members != null) {
                conv.M.AddRange(members);
            }
            if (!string.IsNullOrEmpty(name)) {
                conv.N = name;
            }
            if (properties != null) {
                conv.Attr = new JsonObjectMessage {
                    Data = JsonConvert.SerializeObject(properties)
                };
            }
            command.ConvMessage = conv;
            GenericCommand response = await connection.SendRequest(command);
            LCIMConversation conversation = GetOrCreateConversation(response.ConvMessage.Cid);
            conversation.MergeFrom(response.ConvMessage);
            conversationDict[conversation.Id] = conversation;
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
            LCIMConversationQuery query = GetQuery();
            query.WhereEqualTo("objectId", id)
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

        private void OnNotification(GenericCommand notification) {
            switch (notification.Cmd) {
                case CommandType.Conv:
                    OnConversationNotification(notification);
                    break;
                case CommandType.Direct:
                    OnDirectNotification(notification.DirectMessage);
                    break;
                default:
                    break;
            }
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
                    OnPropertiesUpdated(conv);
                    break;
                default:
                    break;
            }
        }

        private void OnConversationJoined(ConvCommand conv) {
            LCIMConversation conversation = GetOrCreateConversation(conv.Cid);
            conversation.MergeFrom(conv);
            OnInvited?.Invoke(conversation, conv.InitBy);
        }

        private void OnConversationMembersJoined(ConvCommand conv) {
            LCIMConversation conversation = GetOrCreateConversation(conv.Cid);
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

        private void OnPropertiesUpdated(ConvCommand conv) {
            if (conversationDict.TryGetValue(conv.Cid, out LCIMConversation conversation)) {
                // TODO

            }
        }

        private void OnDirectNotification(DirectCommand direct) {
            LCIMMessage message = null;
            if (direct.HasBinaryMsg) {
                // 二进制消息
                byte[] bytes = direct.BinaryMsg.ToByteArray();
                message = new LCIMBinaryMessage(bytes);
            } else {
                // 文本消息
                string messageData = direct.Msg;
                Dictionary<string, object> msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(messageData);
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
            // TODO 获取对话

            OnMessageReceived?.Invoke(null, message);
        }

        private LCIMConversation GetOrCreateConversation(string convId) {
            if (!conversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                conversation = new LCIMConversation(this);
                conversationDict.Add(convId, conversation);
            }
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
