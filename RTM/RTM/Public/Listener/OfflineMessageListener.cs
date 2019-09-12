using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeanCloud.Realtime
{
    internal class OfflineMessageListener : IAVIMListener
    {
        private EventHandler<AVIMMessageEventArgs> m_OnOfflineMessageReceived;
        public event EventHandler<AVIMMessageEventArgs> OnOfflineMessageReceived
        {
            add
            {
                m_OnOfflineMessageReceived += value;
            }
            remove
            {
                m_OnOfflineMessageReceived -= value;
            }
        }
        public void OnNoticeReceived(AVIMNotice notice)
        {
            if (m_OnOfflineMessageReceived != null)
            {
                var msgStr = notice.RawData["msg"].ToString();
                var iMessage = AVRealtime.FreeStyleMessageClassingController.Instantiate(msgStr, notice.RawData);
                var args = new AVIMMessageEventArgs(iMessage);
                m_OnOfflineMessageReceived.Invoke(this, args);
            }

        }

        public bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "direct") return false;
            if (!notice.RawData.ContainsKey("offline")) return false;
            return true;
        }
    }
}
