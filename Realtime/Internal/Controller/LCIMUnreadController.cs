using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMUnreadController : LCIMController {
        internal LCIMUnreadController(LCIMClient client) : base(client) {
        }

        internal override async Task OnNotification(GenericCommand notification) {
            UnreadCommand unread = notification.UnreadMessage;
            List<LCIMConversation> conversationList = new List<LCIMConversation>();
            foreach (UnreadTuple conv in unread.Convs) {
                // 查询对话
                LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
                conversation.Unread = conv.Unread;
                // TODO 反序列化对话
                // 最后一条消息
                JsonConvert.DeserializeObject<Dictionary<string, object>>(conv.Data);
                conversationList.Add(conversation);
            }
            Client.OnUnreadMessagesCountUpdated?.Invoke(conversationList);
        }
    }
}
