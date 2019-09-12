using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 消息接口
    /// <para>所有消息必须实现这个接口</para>
    /// </summary>
    public interface IAVIMMessage
    {
        /// <summary>
        /// Serialize this instance.
        /// </summary>
        /// <returns>The serialize.</returns>
        string Serialize();

        /// <summary>
        /// Validate the specified msgStr.
        /// </summary>
        /// <returns>The validate.</returns>
        /// <param name="msgStr">Message string.</param>
        bool Validate(string msgStr);

        /// <summary>
        /// Deserialize the specified msgStr.
        /// </summary>
        /// <returns>The deserialize.</returns>
        /// <param name="msgStr">Message string.</param>
        IAVIMMessage Deserialize(string msgStr);

        /// <summary>
        /// Gets or sets the conversation identifier.
        /// </summary>
        /// <value>The conversation identifier.</value>
        string ConversationId { get; set; }

        /// <summary>
        /// Gets or sets from client identifier.
        /// </summary>
        /// <value>From client identifier.</value>
        string FromClientId { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the server timestamp.
        /// </summary>
        /// <value>The server timestamp.</value>
        long ServerTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the rcp timestamp.
        /// </summary>
        /// <value>The rcp timestamp.</value>
        long RcpTimestamp { get; set; }

        long UpdatedAt { get; set; }


        #region mention features.
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:LeanCloud.Realtime.IAVIMMessage"/> mention all.
        /// </summary>
        /// <value><c>true</c> if mention all; otherwise, <c>false</c>.</value>
        bool MentionAll { get; set; }

        /// <summary>
        /// Gets or sets the mention list.
        /// </summary>
        /// <value>The mention list.</value>
        IEnumerable<string> MentionList { get; set; }
        #endregion

    }
}
