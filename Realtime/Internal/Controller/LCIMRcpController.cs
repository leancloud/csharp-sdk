using System.Threading.Tasks;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMRcpController : LCIMController {
        internal LCIMRcpController(LCIMClient client) : base(client) {

        }

        #region 消息处理

        internal override async Task OnNotification(GenericCommand notification) {
            RcpCommand rcp = notification.RcpMessage;
            string convId = rcp.Cid;
            string msgId = rcp.Id;
            long timestamp = rcp.T;
            bool isRead = rcp.Read;
            string fromId = rcp.From;
            LCIMConversation conversation = await Client.GetOrQueryConversation(convId);
            if (isRead) {
                Client.OnMessageRead?.Invoke(conversation, msgId);
            } else {
                Client.OnMessageDelivered?.Invoke(conversation, msgId);
            }
        }

        #endregion
    }
}
