using LeanCloud.Storage.Internal;
using LeanCloud.Realtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 对话中成员变动的事件参数，它提供被操作的对话（Conversation），操作类型（AffectedType）
    /// 受影响的成员列表（AffectedMembers）
    /// </summary>
    public class AVIMOnMembersChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 本次成员变动中被操作的具体对话（AVIMConversation）的对象
        /// </summary>
        public AVIMConversation Conversation { get; set; }

        /// <summary>
        /// 变动的类型
        /// </summary>
        public AVIMConversationEventType AffectedType { get; internal set; }

        /// <summary>
        /// 受影响的成员的 Client Ids
        /// </summary>
        public IList<string> AffectedMembers { get; set; }

        /// <summary>
        /// 操作人的 Client ClientId
        /// </summary>
        public string Oprator { get; set; }

        /// <summary>
        /// 操作的时间，已转化为本地时间
        /// </summary>
        public DateTime OpratedTime { get; set; }
    }

    /// <summary>
    /// 变动的类型，目前支持如下：
    /// 1、Joined：当前 Client 主动加入，案例：当 A 主动加入到对话，A 将收到 Joined 事件响应，其余的成员收到 MembersJoined 事件响应
    /// 2、Left：当前 Client 主动退出，案例：当 A 从对话中退出，A 将收到 Left 事件响应，其余的成员收到 MembersLeft 事件响应
    /// 3、MembersJoined：某个成员加入（区别于Joined和Kicked），案例：当 A 把 B 加入到对话中，C 将收到 MembersJoined 事件响应
    /// 4、MembersLeft：某个成员加入（区别于Joined和Kicked），案例：当 A 把 B 从对话中剔除，C 将收到 MembersLeft 事件响应
    /// 5、Invited：当前 Client 被邀请加入，案例：当 A 被 B 邀请加入到对话中，A 将收到 Invited 事件响应，B 将收到 Joined ，其余的成员收到 MembersJoined 事件响应
    /// 6、Kicked：当前 Client 被剔除，案例：当 A 被 B 从对话中剔除，A 将收到 Kicked 事件响应，B 将收到 Left，其余的成员收到 MembersLeft 事件响应
    /// </summary>
    public enum AVIMConversationEventType
    {
        /// <summary>
        /// 自身主动加入
        /// </summary>
        Joined = 1,
        /// <summary>
        /// 自身主动离开
        /// </summary>
        Left,
        /// <summary>
        /// 他人加入
        /// </summary>
        MembersJoined,
        /// <summary>
        /// 他人离开
        /// </summary>
        MembersLeft,
        /// <summary>
        /// 自身被邀请加入
        /// </summary>
        Invited,
        /// <summary>
        /// 自身被他人剔除
        /// </summary>
        Kicked
    }

    #region AVIMMembersJoinListener
    //when Members joined or invited by member,this listener will invoke AVIMOnMembersJoinedEventArgs event.
    /// <summary>
    /// 对话中有成员加入的时候，在改对话中的其他成员都会触发 <see cref="AVIMMembersJoinListener.OnMembersJoined"/> 事件
    /// </summary>
    public class AVIMMembersJoinListener : IAVIMListener
    {

        private EventHandler<AVIMOnMembersJoinedEventArgs> m_OnMembersJoined;
        /// <summary>
        /// 有成员加入到对话时，触发的事件
        /// </summary>
        public event EventHandler<AVIMOnMembersJoinedEventArgs> OnMembersJoined
        {
            add
            {
                m_OnMembersJoined += value;
            }
            remove
            {
                m_OnMembersJoined -= value;
            }
        }

        public virtual void OnNoticeReceived(AVIMNotice notice)
        {
            if (m_OnMembersJoined != null)
            {
                var joinedMembers = AVDecoder.Instance.DecodeList<string>(notice.RawData["m"]);
                var ivitedBy = notice.RawData["initBy"].ToString();
                var conersationId = notice.RawData["cid"].ToString();
                var args = new AVIMOnMembersJoinedEventArgs()
                {
                    ConversationId = conersationId,
                    InvitedBy = ivitedBy,
                    JoinedMembers = joinedMembers
                };
                m_OnMembersJoined.Invoke(this, args);
            }
        }

        public virtual bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "conv") return false;
            if (!notice.RawData.ContainsKey("op")) return false;
            var op = notice.RawData["op"].ToString();
            if (!op.Equals("members-joined")) return false;
            return true;
        }
    }
    #endregion 

    #region AVIMMembersLeftListener
    //  when Members left or kicked by member,this listener will invoke AVIMOnMembersJoinedEventArgs event.
    /// <summary>
    /// 对话中有成员加入的时候，在改对话中的其他成员都会触发 <seealso cref="AVIMMembersLeftListener.OnMembersLeft"/>OnMembersJoined 事件
    /// </summary>
    public class AVIMMembersLeftListener : IAVIMListener
    {
        private EventHandler<AVIMOnMembersLeftEventArgs> m_OnMembersLeft;
        /// <summary>
        /// 有成员加入到对话时，触发的事件
        /// </summary>
        public event EventHandler<AVIMOnMembersLeftEventArgs> OnMembersLeft
        {
            add
            {
                m_OnMembersLeft += value;
            }
            remove
            {
                m_OnMembersLeft -= value;
            }
        }
        public virtual void OnNoticeReceived(AVIMNotice notice)
        {
            if (m_OnMembersLeft != null)
            {
                var leftMembers = AVDecoder.Instance.DecodeList<string>(notice.RawData["m"]);
                var kickedBy = notice.RawData["initBy"].ToString();
                var conersationId = notice.RawData["cid"].ToString();
                var args = new AVIMOnMembersLeftEventArgs()
                {
                    ConversationId = conersationId,
                    KickedBy = kickedBy,
                    LeftMembers = leftMembers
                };
                m_OnMembersLeft.Invoke(this, args);
            }
        }

        public virtual bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "conv") return false;
            if (!notice.RawData.ContainsKey("op")) return false;
            var op = notice.RawData["op"].ToString();
            if (!op.Equals("members-left")) return false;
            return true;
        }
    }
    #endregion

    #region AVIMInvitedListener
    public class AVIMInvitedListener : IAVIMListener
    {
        private EventHandler<AVIMOnInvitedEventArgs> m_OnInvited;
        public event EventHandler<AVIMOnInvitedEventArgs> OnInvited { 
            add {
                m_OnInvited += value;
            } remove {
                m_OnInvited -= value;
            }
        }
        public void OnNoticeReceived(AVIMNotice notice)
        {
            if (m_OnInvited != null)
            {
                var ivitedBy = notice.RawData["initBy"].ToString();
                var conersationId = notice.RawData["cid"].ToString();
                var args = new AVIMOnInvitedEventArgs()
                {
                    ConversationId = conersationId,
                    InvitedBy = ivitedBy,
                };
                m_OnInvited.Invoke(this, args);
            }
        }

        public bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "conv") return false;
            if (!notice.RawData.ContainsKey("op")) return false;
            var op = notice.RawData["op"].ToString();
            if (!op.Equals("joined")) return false;
            return true;
        }
    }
    #endregion

    #region AVIMKickedListener
    public class AVIMKickedListener : IAVIMListener
    {
        private EventHandler<AVIMOnKickedEventArgs> m_OnKicked;
        public event EventHandler<AVIMOnKickedEventArgs> OnKicked { 
            add {
                m_OnKicked += value;
            } remove {
                m_OnKicked -= value;
            }
        }
        public void OnNoticeReceived(AVIMNotice notice)
        {
            if (m_OnKicked != null)
            {
                var kickcdBy = notice.RawData["initBy"].ToString();
                var conersationId = notice.RawData["cid"].ToString();
                var args = new AVIMOnKickedEventArgs()
                {
                    ConversationId = conersationId,
                    KickedBy = kickcdBy,
                };
                m_OnKicked.Invoke(this, args);
            }
        }

        public bool ProtocolHook(AVIMNotice notice)
        {
            if (notice.CommandName != "conv") return false;
            if (!notice.RawData.ContainsKey("op")) return false;
            var op = notice.RawData["op"].ToString();
            if (!op.Equals("left")) return false;
            return true;
        }
    }
    #endregion

}
