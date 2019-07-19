using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeanCloud;
using System.Reflection;
using LeanCloud.Storage.Internal;
using System.Threading;
using System.Collections;
using LeanCloud.Realtime.Internal;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 实时消息的核心基类，它是 Json schema 消息的父类
    /// </summary>
    [AVIMMessageClassName("_AVIMMessage")]
    public class AVIMMessage : IAVIMMessage
    {
        /// <summary>
        /// 默认的构造函数
        /// </summary>
        public AVIMMessage()
        {

        }
        internal readonly object mutex = new object();

        /// <summary>
        /// 对话的Id
        /// </summary>
        public string ConversationId { get; set; }

        /// <summary>
        /// 发送消息的 ClientId
        /// </summary>
        public string FromClientId { get; set; }

        /// <summary>
        /// 消息在全局的唯一标识Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 服务器端的时间戳
        /// </summary>
        public long ServerTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        /// <value>The content.</value>
        public string Content { get; set; }

        /// <summary>
        /// 对方收到消息的时间戳，如果是多人聊天，那以最早收到消息的人回发的 ACK 为准
        /// </summary>
        public long RcpTimestamp { get; set; }

        public long UpdatedAt { get; set; }

        internal string cmdId { get; set; }

        #region
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:LeanCloud.Realtime.IAVIMMessage"/> mention all.
        /// </summary>
        /// <value><c>true</c> if mention all; otherwise, <c>false</c>.</value>
        public bool MentionAll { get; set; }

        /// <summary>
        /// Gets or sets the mention list.
        /// </summary>
        /// <value>The mention list.</value>
        public IEnumerable<string> MentionList { get; set; }

        #endregion

        #region register convertor for custom typed message

        /// <summary>
        /// Serialize this message.
        /// </summary>
        /// <returns>The serialize.</returns>
        public virtual string Serialize()
        {
            return Content;
        }

        /// <summary>
        /// Validate the specified msgStr.
        /// </summary>
        /// <returns>The validate.</returns>
        /// <param name="msgStr">Message string.</param>
        public virtual bool Validate(string msgStr)
        {
            return true;
        }

        /// <summary>
        /// Deserialize the specified msgStr to message subclass instance
        /// </summary>
        /// <returns>The deserialize.</returns>
        /// <param name="msgStr">Message string.</param>
        public virtual IAVIMMessage Deserialize(string msgStr)
        {
            Content = msgStr;
            return this;
        }

        internal virtual MessageCommand BeforeSend(MessageCommand cmd)
        {
            return cmd;
        }

        internal static IAVIMMessage CopyMetaData(IAVIMMessage srcMsg, IAVIMMessage desMsg) {
            if (srcMsg == null)
                return desMsg;

            desMsg.ConversationId = srcMsg.ConversationId;
            desMsg.FromClientId = srcMsg.FromClientId;
            desMsg.Id = srcMsg.Id;
            desMsg.ServerTimestamp = srcMsg.ServerTimestamp;
            desMsg.RcpTimestamp = srcMsg.RcpTimestamp;
            desMsg.UpdatedAt = srcMsg.UpdatedAt;
            return desMsg;
        }

        #endregion
    }


    /// <summary>
    /// 消息的发送选项
    /// </summary>
    public struct AVIMSendOptions
    {
        /// <summary>
        /// 是否需要送达回执
        /// </summary>
        public bool Receipt;
        /// <summary>
        /// 是否是暂态消息，暂态消息不返回送达回执(ack)，不保留离线消息，不触发离线推送
        /// </summary>
        public bool Transient;
        /// <summary>
        /// 消息的优先级，默认是1，可选值还有 2|3
        /// </summary>
        public int Priority;
        /// <summary>
        /// 是否为 Will 类型的消息，这条消息会被缓存在服务端，一旦当前客户端下线，这条消息会被发送到对话内的其他成员
        /// </summary>
        public bool Will;

        /// <summary>
        /// 如果消息的接收者已经下线了，这个字段的内容就会被离线推送到接收者
        ///<remarks>例如，一张图片消息的离线消息内容可以类似于：[您收到一条图片消息，点击查看] 这样的推送内容，参照微信的做法</remarks> 
        /// </summary>
        public IDictionary<string, object> PushData;
    }
}
