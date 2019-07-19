using LeanCloud.Realtime.Internal;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 默认的消息监听器，它主要承担的指责是回执的发送与用户自定义的监听器不冲突
    /// </summary>
    public class AVIMMessageListener : IAVIMListener
    {
        /// <summary>
        /// 默认的 AVIMMessageListener 只会监听 direct 协议，但是并不会触发针对消息类型的判断的监听器
        /// </summary>
        public AVIMMessageListener()
        {

        }

        /// <summary>
        /// Protocols the hook.
        /// </summary>
        /// <returns><c>true</c>, if hook was protocoled, <c>false</c> otherwise.</returns>
        /// <param name="notice">Notice.</param>
        public virtual bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "direct") return false;
            if (notice.RawData.ContainsKey("offline")) return false;
            return true;
        }

        private EventHandler<AVIMMessageEventArgs> m_OnMessageReceived;
        /// <summary>
        /// 接收到聊天消息的事件通知
        /// </summary>
        public event EventHandler<AVIMMessageEventArgs> OnMessageReceived
        {
            add
            {
                m_OnMessageReceived += value;
            }
            remove
            {
                m_OnMessageReceived -= value;
            }
        }
        internal virtual void OnMessage(AVIMNotice notice)
        {
            if (m_OnMessageReceived != null)
            {
                var msgStr = notice.RawData["msg"].ToString();
                var iMessage = AVRealtime.FreeStyleMessageClassingController.Instantiate(msgStr, notice.RawData);
                //var messageNotice = new AVIMMessageNotice(notice.RawData);
                //var messaegObj = AVIMMessage.Create(messageNotice);
                var args = new AVIMMessageEventArgs(iMessage);
                m_OnMessageReceived.Invoke(this, args);
            }
        }

        /// <summary>
        /// Ons the notice received.
        /// </summary>
        /// <param name="notice">Notice.</param>
        public virtual void OnNoticeReceived(AVIMNotice notice)
        {
            this.OnMessage(notice);
        }

    }

    /// <summary>
    /// 文本消息监听器
    /// </summary>
    public class AVIMTextMessageListener : IAVIMListener
    {
        /// <summary>
        /// 构建默认的文本消息监听器
        /// </summary>
        public AVIMTextMessageListener()
        {

        }

        /// <summary>
        /// 构建文本消息监听者
        /// </summary>
        /// <param name="textMessageReceived"></param>
        public AVIMTextMessageListener(Action<AVIMTextMessage> textMessageReceived)
        {
            OnTextMessageReceived += (sender, textMessage) =>
            {
                textMessageReceived(textMessage.TextMessage);
            };
        }

        private EventHandler<AVIMTextMessageEventArgs> m_OnTextMessageReceived;
        public event EventHandler<AVIMTextMessageEventArgs> OnTextMessageReceived
        {
            add
            {
                m_OnTextMessageReceived += value;
            }
            remove
            {
                m_OnTextMessageReceived -= value;
            }
        }

        public virtual bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "direct") return false;
            try
            {
                var msg = Json.Parse(notice.RawData["msg"].ToString()) as IDictionary<string, object>;
                if (!msg.Keys.Contains(AVIMProtocol.LCTYPE)) return false;
                var typInt = 0;
                int.TryParse(msg[AVIMProtocol.LCTYPE].ToString(), out typInt);
                if (typInt != -1) return false;
                return true;
            }
            catch(ArgumentException)
            {
                
            }
            return false;
           
        }

        public virtual void OnNoticeReceived(AVIMNotice notice)
        {
            if (m_OnTextMessageReceived != null)
            {
                var textMessage = new AVIMTextMessage();
                textMessage.Deserialize(notice.RawData["msg"].ToString());
                m_OnTextMessageReceived(this, new AVIMTextMessageEventArgs(textMessage));
            }
        }
    }
}
