using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    internal delegate void OnMessagePatch(IEnumerable<IAVIMMessage> messages);
    internal class MessagePatchListener : IAVIMListener
    {
        public OnMessagePatch OnReceived { get; set; }

        public void OnNoticeReceived(AVIMNotice notice)
        {
            ICollection<IAVIMMessage> patchedMessages = new List<IAVIMMessage>();
            var msgObjs = notice.RawData["patches"] as IList<object>;
            if (msgObjs != null)
            {
                foreach (var msgObj in msgObjs)
                {
                    var msgData = msgObj as IDictionary<string, object>;
                    if (msgData != null)
                    {
                        var msgStr = msgData["data"] as string;
                        var message = AVRealtime.FreeStyleMessageClassingController.Instantiate(msgStr, msgData);
                        patchedMessages.Add(message);
                    }
                }
            }
            if (OnReceived != null)
            {
                if (patchedMessages.Count > 0)
                {
                    this.OnReceived(patchedMessages);
                }
            }
        }

        public bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "patch") return false;
            if (!notice.RawData.ContainsKey("op")) return false;
            if (notice.RawData["op"].ToString() != "modify") return false;
            return true;
        }
    }
}
