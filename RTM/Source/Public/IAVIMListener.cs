using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// WebSocket 监听服务端事件通知的接口
    /// 所有基于协议层的事件监听都需要实现这个接口，然后自定义监听协议。
    /// </summary>
    public interface IAVIMListener
    {
        /// <summary>
        /// 监听的协议 Hook
        /// 例如，消息的协议是 direct 命令，因此消息监听需要判断 <see cref="AVIMNotice.CommandName"/> == "direct" 才可以调用
        /// </summary>
        /// <param name="notice"></param>
        /// <returns></returns>
        bool ProtocolHook(AVIMNotice notice);

        ///// <summary>
        /////  如果 <see cref="IAVIMListener.HookFilter"/> 返回 true，则会启动 NoticeAction 里面的回调逻辑
        ///// </summary>
        //Action<AVIMNotice> NoticeAction { get; set; }

        /// <summary>
        ///  如果 <see cref="IAVIMListener.OnNoticeReceived(AVIMNotice)"/> 返回 true，则会启动 NoticeAction 里面的回调逻辑
        /// </summary>
        void OnNoticeReceived(AVIMNotice notice);
    }
}
