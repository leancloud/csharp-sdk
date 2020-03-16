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
        internal LCWebSocketClient client;

        private Dictionary<string, LCIMConversation> conversationDict;

        public string ClientId {
            get; private set;
        }

        /// <summary>
        /// 当前用户被加入某个对话的黑名单
        /// </summary>
        public Action OnBlocked {
            get; set;
        }

        /// <summary>
        /// 当前客户端被服务端强行下线
        /// </summary>
        public Action OnClosed {
            get; set;
        }

        /// <summary>
        /// 客户端连接断开
        /// </summary>
        public Action OnDisconnected {
            get; set;
        }

        /// <summary>
        /// 客户端连接恢复正常
        /// </summary>
        public Action OnReconnect {
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

        public LCIMClient(string clientId) {
            ClientId = clientId;
            conversationDict = new Dictionary<string, LCIMConversation>();
        }

        public async Task Open() {
            client = new LCWebSocketClient {
                OnNotification = OnNotification
            };
            await client.Connect();
            // Open Session
            GenericCommand command = NewCommand(CommandType.Session, OpType.Open);
            command.SessionMessage = new SessionCommand();
            await client.SendRequest(command);
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
            GenericCommand response = await client.SendRequest(command);
            LCIMConversation conversation = GetOrCreateConversation(response.ConvMessage.Cid);
            conversation.MergeFrom(response.ConvMessage);
            conversationDict[conversation.Id] = conversation;
            return conversation;
        }

        public async Task<LCIMConversation> GetConversation(string id) {
            return null;
        }

        public async Task<List<LCIMConversation>> GetConversationList(List<string> idList) {
            return null;
        }

        public async Task<LCIMConversationQuery> GetConversationQuery() {
            return null;
        }

        private void OnNotification(GenericCommand notification) {
            switch (notification.Cmd) {
                case CommandType.Conv:
                    OnConversationNotification(notification);
                    break;
                default:
                    break;
            }
        }

        private void OnConversationNotification(GenericCommand notification) {
            switch (notification.Op) {
                case OpType.Joined:
                    OnConversationJoined(notification.ConvMessage);
                    break;
                case OpType.MembersJoined:
                    OnConversationMembersJoined(notification.ConvMessage);
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
