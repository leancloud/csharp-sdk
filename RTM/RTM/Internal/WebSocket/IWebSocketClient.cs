using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    /// <summary>
    /// LeanCloud WebSocket 客户端接口
    /// </summary>
    public interface IWebSocketClient {
        /// <summary>
        /// 客户端 WebSocket 长连接是否打开
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// WebSocket 长连接关闭时触发的事件回调
        /// </summary>
        event Action<int, string, string> OnClosed;

        /// <summary>
        /// 云端发送数据包给客户端，WebSocket 接受到时触发的事件回调
        /// </summary>
        event Action<string> OnMessage;

        /// <summary>
        /// 客户端 WebSocket 长连接成功打开时，触发的事件回调
        /// </summary>
        event Action OnOpened;

        /// <summary>
        /// 主动关闭连接
        /// </summary>
        void Close();

        void Disconnect();

        /// <summary>
        /// 打开连接
        /// </summary>
        /// <param name="url">wss 地址</param>
        /// <param name="protocol">子协议</param>
        void Open(string url, string protocol = null);
        /// <summary>
        /// 发送数据包的接口
        /// </summary>
        /// <param name="message"></param>
        void Send(string message);

        Task<bool> Connect(string url, string protocol = null);
    }
}
