using LeanCloud.Realtime.Internal;
using LeanCloud.Storage.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 纯文本信息
    /// </summary>
    [AVIMMessageClassName("_AVIMTextMessage")]
    [AVIMTypedMessageTypeInt(-1)]
    public class AVIMTextMessage : AVIMTypedMessage
    {
        /// <summary>
        /// 构建一个文本信息 <see cref="AVIMTextMessage"/> class.
        /// </summary>
        public AVIMTextMessage()
        {

        }

        /// <summary>
        /// 文本类型标记
        /// </summary>
        [Obsolete("LCType is deprecated, please use AVIMTypedMessageTypeInt instead.")]
        [AVIMMessageFieldName("_lctype")]
        public int LCType
        {
            get; set;
        }

        /// <summary>
        /// 构造一个纯文本信息
        /// </summary>
        /// <param name="textContent"></param>
        public AVIMTextMessage(string textContent)
            : this()
        {
            TextContent = textContent;
        }
    }
}
