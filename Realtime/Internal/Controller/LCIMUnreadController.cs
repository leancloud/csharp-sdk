using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMUnreadController : LCIMController {
        internal LCIMUnreadController(LCIMClient client) : base(client) {
        }

        internal override async Task OnNotification(GenericCommand notification) {
            UnreadCommand unread = notification.UnreadMessage;

            IEnumerable<string> convIds = unread.Convs
                .Select(conv => conv.Cid);
            Dictionary<string, LCIMConversation> conversations = (await Client.GetConversationList(convIds))
                .ToDictionary(item => item.Id);
            List<LCIMConversation> conversationList = unread.Convs.Select(conv => {
                LCIMConversation conversation = conversations[conv.Cid];
                conversation.Unread = conv.Unread;
                // 解析最后一条消息
                Dictionary<string, object> msgData = JsonConvert.DeserializeObject<Dictionary<string, object>>(conv.Data,
                    new LCJsonConverter());
                int msgType = (int)msgData["_lctype"];
                LCIMMessage message = null;
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
                message.ConversationId = conv.Cid;
                message.Id = conv.Mid;
                message.FromClientId = conv.From;
                message.SentTimestamp = conv.Timestamp;
                conversation.LastMessage = message;
                return conversation;
            }).ToList();
            Client.OnUnreadMessagesCountUpdated?.Invoke(conversationList);
        }
    }
}
