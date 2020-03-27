using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMUnreadController : LCIMController {
        internal LCIMUnreadController(LCIMClient client) : base(client) {

        }

        #region 消息处理

        internal override async Task OnNotification(GenericCommand notification) {
            UnreadCommand unread = notification.UnreadMessage;

            IEnumerable<string> convIds = unread.Convs
                .Select(conv => conv.Cid);
            Dictionary<string, LCIMConversation> conversationDict = (await Client.GetConversationList(convIds))
                .ToDictionary(item => item.Id);
            ReadOnlyCollection<LCIMConversation> conversations = unread.Convs.Select(conv => {
                // 设置对话中的未读数据
                LCIMConversation conversation = conversationDict[conv.Cid];
                conversation.Unread = conv.Unread;

                LCIMMessage message = null;
                if (conv.HasBinaryMsg) {
                    // 二进制消息
                    byte[] bytes = conv.BinaryMsg.ToByteArray();
                    message = LCIMBinaryMessage.Deserialize(bytes);
                } else {
                    // 类型消息
                    message = LCIMTypedMessage.Deserialize(conv.Data);
                }
                // 填充消息数据
                message.ConversationId = conv.Cid;
                message.Id = conv.Mid;
                message.FromClientId = conv.From;
                message.SentTimestamp = conv.Timestamp;
                conversation.LastMessage = message;
                return conversation;
            }).ToList().AsReadOnly();
            Client.OnUnreadMessagesCountUpdated?.Invoke(conversations);
        }

        #endregion
    }
}
