using LeanCloud;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime
{
    /// <summary>
    /// 代表一个实时通信的终端用户
    /// </summary>
    public class AVIMClient
    {
        private readonly string clientId;
        private readonly AVRealtime _realtime;
        internal readonly object mutex = new object();
        internal readonly object patchMutex = new object();

        /// <summary>
        /// 一些可变的配置选项，便于应对各种需求场景
        /// </summary>
        public struct Configuration
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="T:LeanCloud.Realtime.AVIMClient.Configuration"/>
            /// auto read.
            /// </summary>
            /// <value><c>true</c> if auto read; otherwise, <c>false</c>.</value>
            public bool AutoRead { get; set; }
        }

        /// <summary>
        /// Gets or sets the current configuration.
        /// </summary>
        /// <value>The current configuration.</value>
        public Configuration CurrentConfiguration
        {
            get; set;
        }

        internal AVRealtime LinkedRealtime
        {
            get { return _realtime; }
        }

        /// <summary>
        /// 单点登录所使用的 Tag
        /// </summary>
        public string Tag
        {
            get;
            private set;
        }

        /// <summary>
        /// 客户端的标识,在一个 Application 内唯一。
        /// </summary>
        public string ClientId
        {
            get { return clientId; }
        }

        //private EventHandler<AVIMNotice> m_OnNoticeReceived;
        ///// <summary>
        ///// 接收到服务器的命令时触发的事件
        ///// </summary>
        //public event EventHandler<AVIMNotice> OnNoticeReceived
        //{
        //    add
        //    {
        //        m_OnNoticeReceived += value;
        //    }
        //    remove
        //    {
        //        m_OnNoticeReceived -= value;
        //    }
        //}

        private int onMessageReceivedCount = 0;
        private EventHandler<AVIMMessageEventArgs> m_OnMessageReceived;
        /// <summary>
        /// 接收到聊天消息的事件通知
        /// </summary>
        public event EventHandler<AVIMMessageEventArgs> OnMessageReceived
        {
            add
            {
                onMessageReceivedCount++;
                AVRealtime.PrintLog("AVIMClient.OnMessageReceived event add with " + onMessageReceivedCount + " times");
                m_OnMessageReceived += value;
            }
            remove
            {
                onMessageReceivedCount--;
                AVRealtime.PrintLog("AVIMClient.OnMessageReceived event remove with" + onMessageReceivedCount + " times");
                m_OnMessageReceived -= value;
            }
        }

        /// <summary>
        /// Occurs when on members joined.
        /// </summary>
        public event EventHandler<AVIMOnMembersJoinedEventArgs> OnMembersJoined;

        /// <summary>
        /// Occurs when on members left.
        /// </summary>
        public event EventHandler<AVIMOnMembersLeftEventArgs> OnMembersLeft;

        /// <summary>
        /// Occurs when on kicked.
        /// </summary>
        public event EventHandler<AVIMOnKickedEventArgs> OnKicked;

        /// <summary>
        /// Occurs when on invited.
        /// </summary>
        public event EventHandler<AVIMOnInvitedEventArgs> OnInvited;

        internal event EventHandler<AVIMMessageEventArgs> OnOfflineMessageReceived;

        private EventHandler<AVIMSessionClosedEventArgs> m_OnSessionClosed;
        /// <summary>
        /// 当前打开的链接被迫关闭时触发的事件回调
        /// <remarks>可能的原因有单点登录冲突，或者被 REST API 强制踢下线</remarks>
        /// </summary>
        public event EventHandler<AVIMSessionClosedEventArgs> OnSessionClosed
        {
            add
            {
                m_OnSessionClosed += value;
            }
            remove
            {
                m_OnSessionClosed -= value;
            }
        }

        /// <summary>
        /// 创建 AVIMClient 对象
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="realtime"></param>
        internal AVIMClient(string clientId, AVRealtime realtime)
            : this(clientId, null, realtime)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="tag"></param>
        /// <param name="realtime"></param>
        internal AVIMClient(string clientId, string tag, AVRealtime realtime)
        {
            this.clientId = clientId;
            Tag = tag ?? tag;
            _realtime = realtime;

            #region sdk 强制在接收到消息之后一定要向服务端回发 ack
            var ackListener = new AVIMMessageListener();
            ackListener.OnMessageReceived += AckListener_OnMessageReceieved;
            //this.RegisterListener(ackListener);
            #endregion

            #region 默认要为当前 client 绑定一个消息的监听器，用作消息的事件通知
            var messageListener = new AVIMMessageListener();
            messageListener.OnMessageReceived += MessageListener_OnMessageReceived;
            this.RegisterListener(messageListener);
            #endregion

            #region 默认要为当前 client 绑定一个 session close 的监听器，用来监测单点登录冲突的事件通知
            var sessionListener = new SessionListener();
            sessionListener.OnSessionClosed += SessionListener_OnSessionClosed;
            this.RegisterListener(sessionListener);
            #endregion

            #region 默认要为当前 client 监听 Ta 所出的对话中的人员变动的被动消息通知
            var membersJoinedListener = new AVIMMembersJoinListener();
            membersJoinedListener.OnMembersJoined += MembersJoinedListener_OnMembersJoined;
            this.RegisterListener(membersJoinedListener);

            var membersLeftListener = new AVIMMembersLeftListener();
            membersLeftListener.OnMembersLeft += MembersLeftListener_OnMembersLeft;
            this.RegisterListener(membersLeftListener);

            var invitedListener = new AVIMInvitedListener();
            invitedListener.OnInvited += InvitedListener_OnInvited;
            this.RegisterListener(invitedListener);

            var kickedListener = new AVIMKickedListener();
            kickedListener.OnKicked += KickedListener_OnKicked;
            this.RegisterListener(kickedListener);
            #endregion

            #region 当前 client id 离线的时间内，TA 所在的对话产生的普通消息会以离线消息的方式送达到 TA 下一次登录的客户端
            var offlineMessageListener = new OfflineMessageListener();
            offlineMessageListener.OnOfflineMessageReceived += OfflineMessageListener_OnOfflineMessageReceived;
            this.RegisterListener(offlineMessageListener);
            #endregion

            #region 当前 client 离线期间内产生的未读消息可以通过之后调用 Conversation.SyncStateAsync 获取一下离线期间内的未读状态
            var unreadListener = new ConversationUnreadListener();
            this.RegisterListener(unreadListener);
            #endregion

            #region 消息补丁（修改或者撤回）
            var messagePatchListener = new MessagePatchListener();
            messagePatchListener.OnReceived = (messages) =>
            {
                foreach (var message in messages) {
                    if (message is AVIMRecalledMessage) {
                        m_OnMessageRecalled?.Invoke(this, new AVIMMessagePatchEventArgs(message));
                    } else {
                        m_OnMessageUpdated?.Invoke(this, new AVIMMessagePatchEventArgs(message));
                    }
                }
            };
            this.RegisterListener(messagePatchListener);
            #endregion

            #region configuration
            CurrentConfiguration = new Configuration()
            {
                AutoRead = true,
            };
            #endregion

        }

        private void OfflineMessageListener_OnOfflineMessageReceived(object sender, AVIMMessageEventArgs e)
        {
            if (OnOfflineMessageReceived != null)
            {
                OnOfflineMessageReceived(this, e);
            }
            this.AckListener_OnMessageReceieved(sender, e);
        }

        private void KickedListener_OnKicked(object sender, AVIMOnKickedEventArgs e)
        {
            if (OnKicked != null)
                OnKicked(this, e);
        }

        private void InvitedListener_OnInvited(object sender, AVIMOnInvitedEventArgs e)
        {
            if (OnInvited != null)
                OnInvited(this, e);
        }

        private void MembersLeftListener_OnMembersLeft(object sender, AVIMOnMembersLeftEventArgs e)
        {
            if (OnMembersLeft != null)
                OnMembersLeft(this, e);
        }

        private void MembersJoinedListener_OnMembersJoined(object sender, AVIMOnMembersJoinedEventArgs e)
        {
            if (OnMembersJoined != null)
                OnMembersJoined(this, e);
        }

        private void SessionListener_OnSessionClosed(int arg1, string arg2, string arg3)
        {
            if (m_OnSessionClosed != null)
            {
                var args = new AVIMSessionClosedEventArgs()
                {
                    Code = arg1,
                    Reason = arg2,
                    Detail = arg3
                };
                if (args.Code == 4115 || args.Code == 4111)
                {
                    this._realtime.sessionConflict = true;
                }

                m_OnSessionClosed(this, args);
            }
            AVRealtime.PrintLog("SessionListener_OnSessionClosed invoked.");
            //this.LinkedRealtime.LogOut();
        }

        private void MessageListener_OnMessageReceived(object sender, AVIMMessageEventArgs e)
        {
            if (this.m_OnMessageReceived != null)
            {
                this.m_OnMessageReceived.Invoke(this, e);
            }
            this.AckListener_OnMessageReceieved(sender, e);
        }

        private void AckListener_OnMessageReceieved(object sender, AVIMMessageEventArgs e)
        {
            lock (mutex)
            {
                var ackCommand = new AckCommand().MessageId(e.Message.Id)
                    .ConversationId(e.Message.ConversationId);

                // 在 v.2 协议下，只要在线收到消息，就默认是已读的，下次上线不会再把当前消息当做未读消息
                if (this.LinkedRealtime.CurrentConfiguration.OfflineMessageStrategy == AVRealtime.OfflineMessageStrategy.UnreadNotice)
                {
                    ackCommand = ackCommand.ReadAck();
                }

                this.RunCommandAsync(ackCommand);
            }
        }

        private void UpdateUnreadNotice(object sender, AVIMMessageEventArgs e)
        {
            ConversationUnreadListener.UpdateNotice(e.Message);
        }

        #region listener 

        /// <summary>
        /// 注册 IAVIMListener
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="runtimeHook"></param>
        public void RegisterListener(IAVIMListener listener, Func<AVIMNotice, bool> runtimeHook = null)
        {
            _realtime.SubscribeNoticeReceived(listener, runtimeHook);
        }

        #region get client instance
        /// <summary>
        /// Get the specified clientId.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="clientId">Client identifier.</param>
        public static AVIMClient Get(string clientId)
        {
            if (AVRealtime.clients == null || !AVRealtime.clients.ContainsKey(clientId)) throw new Exception(string.Format("no client found with a id in {0}", clientId));

            return AVRealtime.clients[clientId];
        }
        #endregion

        #endregion
        /// <summary>
        /// 创建对话
        /// </summary>
        /// <param name="conversation">对话</param>
        /// <param name="isUnique">是否创建唯一对话，当 isUnique 为 true 时，如果当前已经有相同成员的对话存在则返回该对话，否则会创建新的对话。该值默认为 false。</param>
        /// <returns></returns>
        internal Task<AVIMConversation> CreateConversationAsync(AVIMConversation conversation, bool isUnique = true)
        {
            var cmd = new ConversationCommand()
                .Generate(conversation)
                .Unique(isUnique);

            var convCmd = cmd.Option("start")
                .PeerId(clientId);

            return LinkedRealtime.AttachSignature(convCmd, LinkedRealtime.SignatureFactory.CreateStartConversationSignature(this.clientId, conversation.MemberIds)).OnSuccess(_ =>
             {
                 return this.RunCommandAsync(convCmd).OnSuccess(t =>
                  {
                      var result = t.Result;
                      if (result.Item1 < 1)
                      {
                          var members = conversation.MemberIds.ToList();
                          members.Add(ClientId);
                          conversation.MemberIds = members;
                          conversation.MergeFromPushServer(result.Item2);
                      }

                      return conversation;
                  });
             }).Unwrap();
        }

        /// <summary>
        /// 创建与目标成员的对话.
        /// </summary>
        /// <returns>返回对话实例.</returns>
        /// <param name="member">目标成员.</param>
        /// <param name="members">目标成员列表.</param>
        /// <param name="name">对话名称.</param>
        /// <param name="isSystem">是否是系统对话.</param>
        /// <param name="isTransient">是否为暂态对话（聊天室）.</param>
        /// <param name="isUnique">是否是唯一对话.</param>
        /// <param name="options">自定义属性.</param>
        public Task<AVIMConversation> CreateConversationAsync(string member = null,
            IEnumerable<string> members = null,
            string name = "",
            bool isSystem = false,
            bool isTransient = false,
            bool isUnique = true,
            bool isTemporary = false,
            int ttl = 86400,
            IDictionary<string, object> options = null)
        {
            if (member == null) member = ClientId;
            var membersAsList = Concat<string>(member, members, "创建对话时被操作的 member(s) 不可以为空。");
            var conversation = new AVIMConversation(members: membersAsList,
                name: name,
                isUnique: isUnique,
                isSystem: isSystem,
                isTransient: isTransient, 
                isTemporary: isTemporary,
                ttl: ttl,
                client: this);
            if (options != null)
            {
                foreach (var key in options.Keys)
                {
                    conversation[key] = options[key];
                }
            }
            return CreateConversationAsync(conversation, isUnique);
        }

        /// <summary>
        /// Creates the conversation async.
        /// </summary>
        /// <returns>The conversation async.</returns>
        /// <param name="builder">Builder.</param>
        public Task<AVIMConversation> CreateConversationAsync(IAVIMConversatioBuilder builder)
        {
            var conversation = builder.Build();
            return CreateConversationAsync(conversation, conversation.IsUnique);
        }

        /// <summary>
        /// Gets the conversatio builder.
        /// </summary>
        /// <returns>The conversatio builder.</returns>
        public AVIMConversationBuilder GetConversationBuilder()
        {
            var builder = AVIMConversationBuilder.CreateDefaultBuilder();
            builder.Client = this;
            return builder;
        }

        /// <summary>
        /// 创建虚拟对话，对话 id 是由本地直接生成，云端根据规则消息发送给指定的 client id(s)
        /// </summary>
        /// <param name="member"></param>
        /// <param name="members"></param>
        /// <param name="ttl">过期时间，默认是一天(86400 秒)，单位是秒</param>
        /// <returns></returns>
        public Task<AVIMConversation> CreateTemporaryConversationAsync(string member = null,
            IEnumerable<string> members = null, int ttl = 86400)
        {
            if (member == null) member = ClientId;
            var membersAsList = Concat<string>(member, members, "创建对话时被操作的 member(s) 不可以为空。");
            return CreateConversationAsync(member, membersAsList, isTemporary: true, ttl: ttl);
        }

        /// <summary>
        /// 创建聊天室（即：暂态对话）
        /// </summary>
        /// <param name="chatroomName">聊天室名称</param>
        /// <returns></returns>
        public Task<AVIMConversation> CreateChatRoomAsync(string chatroomName)
        {
            return CreateConversationAsync(name: chatroomName, isTransient: true);
        }

        /// <summary>
        /// 获取一个对话
        /// </summary>
        /// <param name="id">对话的 ID</param>
        /// <param name="noCache">从服务器获取</param>
        /// <returns></returns>
        public Task<AVIMConversation> GetConversationAsync(string id, bool noCache = true)
        {
            if (!noCache) return Task.FromResult(new AVIMConversation(this) { ConversationId = id });
            else
            {
                return this.GetQuery().WhereEqualTo("objectId", id).FirstAsync();
            }
        }

        #region send message
        /// <summary>
        /// 向目标对话发送消息
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <param name="message">消息体</param>
        /// <returns></returns>
		public Task<IAVIMMessage> SendMessageAsync(
          AVIMConversation conversation,
          IAVIMMessage message)
        {
            return this.SendMessageAsync(conversation, message, new AVIMSendOptions()
            {
                Receipt = true,
                Transient = false,
                Priority = 1,
                Will = false,
                PushData = null,
            });
        }

        /// <summary>
        /// 向目标对话发送消息
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <param name="message">消息体</param>
        /// <param name="options">消息的发送选项，包含了一些特殊的标记<see cref="AVIMSendOptions"/></param>
        /// <returns></returns>
        public Task<IAVIMMessage> SendMessageAsync(
          AVIMConversation conversation,
          IAVIMMessage message,
          AVIMSendOptions options)
        {
            if (this.LinkedRealtime.State != AVRealtime.Status.Online) throw new Exception("未能连接到服务器，无法发送消息。");

            var messageBody = message.Serialize();

            message.ConversationId = conversation.ConversationId;
            message.FromClientId = this.ClientId;

            var cmd = new MessageCommand()
                .Message(messageBody)
                .ConvId(conversation.ConversationId)
                .Receipt(options.Receipt)
                .Transient(options.Transient)
                .Priority(options.Priority)
                .Will(options.Will)
                .MentionAll(message.MentionAll);

            if (message is AVIMMessage)
            {
                cmd = ((AVIMMessage)message).BeforeSend(cmd);
            }

            if (options.PushData != null)
            {
                cmd = cmd.PushData(options.PushData);
            }

            if (message.MentionList != null)
            {
                cmd = cmd.Mention(message.MentionList);
            }

            var directCmd = cmd.PeerId(this.ClientId);

            return this.RunCommandAsync(directCmd).OnSuccess(t =>
            {
                var response = t.Result.Item2;

                message.Id = response["uid"].ToString();
                message.ServerTimestamp = long.Parse(response["t"].ToString());

                return message;

            });
        }


        #endregion

        #region mute & unmute
        /// <summary>
        /// 当前用户对目标对话进行静音操作
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public Task MuteConversationAsync(AVIMConversation conversation)
        {
            var convCmd = new ConversationCommand()
                .ConversationId(conversation.ConversationId)
                .Option("mute")
                .PeerId(this.ClientId);

            return this.RunCommandAsync(convCmd);
        }
        /// <summary>
        /// 当前用户对目标对话取消静音，恢复该对话的离线消息推送
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        public Task UnmuteConversationAsync(AVIMConversation conversation)
        {
            var convCmd = new ConversationCommand()
                .ConversationId(conversation.ConversationId)
                .Option("unmute")
                .PeerId(this.ClientId);

            return this.RunCommandAsync(convCmd);
        }
        #endregion

        #region Conversation members operations
        internal Task OperateMembersAsync(AVIMConversation conversation, string action, string member = null, IEnumerable<string> members = null)
        {
            if (string.IsNullOrEmpty(conversation.ConversationId))
            {
                throw new Exception("conversation id 不可以为空。");
            }

            var membersAsList = Concat<string>(member, members, "加人或者踢人的时候，被操作的 member(s) 不可以为空。");

            var cmd = new ConversationCommand().ConversationId(conversation.ConversationId)
                .Members(membersAsList)
                .Option(action)
                .PeerId(clientId);

            return this.LinkedRealtime.AttachSignature(cmd, LinkedRealtime.SignatureFactory.CreateConversationSignature(conversation.ConversationId, ClientId, membersAsList, ConversationSignatureAction.Add)).OnSuccess(_ =>
            {
                return this.RunCommandAsync(cmd).OnSuccess(t =>
                {
                    var result = t.Result;
                    if (!conversation.IsTransient)
                    {
                        if (conversation.MemberIds == null) conversation.MemberIds = new List<string>();
                        conversation.MemberIds = conversation.MemberIds.Concat(membersAsList);
                    }
                    return result;
                });
            }).Unwrap();
        }
        internal IEnumerable<T> Concat<T>(T single, IEnumerable<T> collection, string exString = null)
        {
            List<T> asList = null;
            if (collection == null)
            {
                collection = new List<T>();
            }
            asList = collection.ToList();
            if (asList.Count == 0 && single == null)
            {
                exString = exString ?? "can not cancat a collection with a null value.";
                throw new ArgumentNullException(exString);
            }
            asList.Add(single);
            return asList;
        }

        #region Join
        /// <summary>
        /// 当前用户加入到目标的对话中
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <returns></returns>
        public Task JoinAsync(AVIMConversation conversation)
        {
            return this.OperateMembersAsync(conversation, "add", this.ClientId);
        }
        #endregion

        #region Invite
        /// <summary>
        /// 直接将其他人加入到目标对话
        /// <remarks>被操作的人会在客户端会触发 OnInvited 事件,而已经存在于对话的用户会触发 OnMembersJoined 事件</remarks>
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <param name="member">单个的 Client Id</param>
        /// <param name="members">Client Id 集合</param>
        /// <returns></returns>
        public Task InviteAsync(AVIMConversation conversation, string member = null, IEnumerable<string> members = null)
        {
            return this.OperateMembersAsync(conversation, "add", member, members);
        }
        #endregion

        #region Left
        /// <summary>
        /// 当前 Client 离开目标对话
        /// <remarks>可以理解为是 QQ 群的退群操作</remarks>
        /// <remarks></remarks>
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <returns></returns>
        [Obsolete("use LeaveAsync instead.")]
        public Task LeftAsync(AVIMConversation conversation)
        {
            return this.OperateMembersAsync(conversation, "remove", this.ClientId);
        }

        /// <summary>
        /// Leaves the conversation async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="conversation">Conversation.</param>
        public Task LeaveAsync(AVIMConversation conversation)
        {
            return this.OperateMembersAsync(conversation, "remove", this.ClientId);
        }
        #endregion

        #region Kick
        /// <summary>
        /// 从目标对话中剔除成员
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <param name="member">被剔除的单个成员</param>
        /// <param name="members">被剔除的成员列表</param>
        /// <returns></returns>
        public Task KickAsync(AVIMConversation conversation, string member = null, IEnumerable<string> members = null)
        {
            return this.OperateMembersAsync(conversation, "remove", member, members);
        }
        #endregion

        #endregion

        #region Query && Message history && ack

        /// <summary>
        /// Get conversation query.
        /// </summary>
        /// <returns></returns>
        public AVIMConversationQuery GetQuery()
        {
            return GetConversationQuery();
        }

        /// <summary>
        /// Get conversation query.
        /// </summary>
        /// <returns>The conversation query.</returns>
        public AVIMConversationQuery GetConversationQuery()
        {
            return new AVIMConversationQuery(this);
        }

        #region load message history

        /// <summary>
        /// 查询目标对话的历史消息
        /// <remarks>不支持聊天室（暂态对话）</remarks>
        /// </summary>
        /// <param name="conversation">目标对话</param>
        /// <param name="beforeMessageId">从 beforeMessageId 开始向前查询（和 beforeTimeStampPoint 共同使用，为防止某毫秒时刻有重复消息）</param>
        /// <param name="afterMessageId"> 截止到某个 afterMessageId (不包含)</param>
        /// <param name="beforeTimeStampPoint">从 beforeTimeStampPoint 开始向前查询</param>
        /// <param name="afterTimeStampPoint">拉取截止到 afterTimeStampPoint 时间戳（不包含）</param>
        /// <param name="direction">查询方向，默认是 1，如果是 1 表示从新消息往旧消息方向， 0 则相反,其他值无效</param>
        /// <param name="limit">拉取消息条数，默认值 20 条，可设置为 1 - 1000 之间的任意整数</param>
        /// <returns></returns>
        public Task<IEnumerable<T>> QueryMessageAsync<T>(AVIMConversation conversation,
            string beforeMessageId = null,
            string afterMessageId = null,
            DateTime? beforeTimeStampPoint = null,
            DateTime? afterTimeStampPoint = null,
            int direction = 1,
            int limit = 20)
            where T : IAVIMMessage
        {
            var maxLimit = 1000;
            var actualLimit = limit > maxLimit ? maxLimit : limit;
            var logsCmd = new AVIMCommand()
                .Command("logs")
                .Argument("cid", conversation.ConversationId)
                .Argument("l", actualLimit);

            if (beforeMessageId != null)
            {
                logsCmd = logsCmd.Argument("mid", beforeMessageId);
            }

            if (afterMessageId != null)
            {
                logsCmd = logsCmd.Argument("tmid", afterMessageId);
            }

            if (beforeTimeStampPoint != null && beforeTimeStampPoint.Value != DateTime.MinValue)
            {
                logsCmd = logsCmd.Argument("t", beforeTimeStampPoint.Value.ToUnixTimeStamp());
            }

            if (afterTimeStampPoint != null && afterTimeStampPoint.Value != DateTime.MinValue)
            {
                logsCmd = logsCmd.Argument("tt", afterTimeStampPoint.Value.ToUnixTimeStamp());
            }

            if (direction == 0)
            {
                logsCmd = logsCmd.Argument("direction", "NEW");
            }

            var subMessageType = typeof(T);
            var subTypeInteger = subMessageType == typeof(AVIMTypedMessage) ? 0 : FreeStyleMessageClassInfo.GetTypedInteger(subMessageType.GetTypeInfo());

            if (subTypeInteger != 0)
            {
                logsCmd = logsCmd.Argument("lctype", subTypeInteger);
            }

            return this.RunCommandAsync(logsCmd).OnSuccess(t =>
            {
                var rtn = new List<IAVIMMessage>();
                var result = t.Result.Item2;
                var logs = result["logs"] as List<object>;
                if (logs != null)
                {
                    foreach (var log in logs)
                    {
                        var logMap = log as IDictionary<string, object>;
                        if (logMap != null)
                        {
                            var msgStr = logMap["data"].ToString();
                            var messageObj = AVRealtime.FreeStyleMessageClassingController.Instantiate(msgStr, logMap);
                            messageObj.ConversationId = conversation.ConversationId;
                            rtn.Add(messageObj);
                        }
                    }
                }

                conversation.OnMessageLoad(rtn);

                return rtn.AsEnumerable().OfType<T>();
            });
        }
        #endregion


        //public Task MarkAsReadAsync(string conversationId = null, string messageId = null, AVIMConversation conversation = null, AVIMMessage message = null)
        //{
        //    var msgId = messageId != null ? messageId : message.Id;
        //    var convId = conversationId != null ? conversationId : conversation.ConversationId;
        //    if (convId == null && msgId == null) throw new ArgumentNullException("发送已读回执的时候，必须指定 conversation id 或者 message id");
        //    lock (mutex)
        //    {
        //        var ackCommand = new AckCommand()
        //                .ReadAck().MessageId(msgId)
        //            .ConversationId(convId)
        //            .PeerId(this.ClientId);

        //        return this.RunCommandAsync(ackCommand);
        //    }
        //}
        #region 查询对话中对方的接收状态，也就是已读回执
        private Task<Tuple<long, long>> FetchAllReceiptTimestampsAsync(string targetClientId = null, string conversationId = null, AVIMConversation conversation = null, bool queryAllMembers = false)
        {
            var convId = conversationId != null ? conversationId : conversation.ConversationId;
            if (convId == null) throw new ArgumentNullException("conversationId 和 conversation 不可以同时为 null");

            var cmd = new ConversationCommand().ConversationId(convId)
              .TargetClientId(targetClientId)
              .QueryAllMembers(queryAllMembers)
              .Option("max-read")
              .PeerId(clientId);

            return this.RunCommandAsync(cmd).OnSuccess(t =>
            {
                var result = t.Result;
                long maxReadTimestamp = -1;
                long maxAckTimestamp = -1;

                if (result.Item2.ContainsKey("maxReadTimestamp"))
                {
                    long.TryParse(result.Item2["maxReadTimestamp"].ToString(), out maxReadTimestamp);
                }
                if (result.Item2.ContainsKey("maxAckTimestamp"))
                {
                    long.TryParse(result.Item2["maxAckTimestamp"].ToString(), out maxAckTimestamp);
                }
                return new Tuple<long, long>(maxAckTimestamp, maxReadTimestamp);

            });
        }
        #endregion

        #region 查询对方是否在线
        /// <summary>
        /// 查询对方 client Id 是否在线
        /// </summary>
        /// <param name="targetClientId">单个 client Id</param>
        /// <param name="targetClientIds">多个 client Id 集合</param>
        /// <returns></returns>
        public Task<IEnumerable<Tuple<string, bool>>> PingAsync(string targetClientId = null, IEnumerable<string> targetClientIds = null)
        {
            List<string> queryIds = null;
            if (targetClientIds != null) queryIds = targetClientIds.ToList();
            if (queryIds == null && string.IsNullOrEmpty(targetClientId)) throw new ArgumentNullException("必须查询至少一个 client id 的状态，targetClientId 和 targetClientIds 不可以同时为空");
            queryIds.Add(targetClientId);

            var cmd = new SessionCommand()
                .SessionPeerIds(queryIds)
                .Option("query");

            return this.RunCommandAsync(cmd).OnSuccess(t =>
            {
                var result = t.Result;
                List<Tuple<string, bool>> rtn = new List<Tuple<string, bool>>();
                var onlineSessionPeerIds = AVDecoder.Instance.DecodeList<string>(result.Item2["onlineSessionPeerIds"]);
                foreach (var peerId in targetClientIds)
                {
                    rtn.Add(new Tuple<string, bool>(peerId, onlineSessionPeerIds.Contains(peerId)));
                }
                return rtn.AsEnumerable();
            });
        }
        #endregion
        #region 获取暂态对话在线人数
        /// <summary>
        /// 获取暂态对话（聊天室）在线人数，依赖缓存，并不一定 100% 与真实数据一致。
        /// </summary>
        /// <param name="chatroomId"></param>
        /// <returns></returns>
        public Task<int> CountOnlineClientsAsync(string chatroomId)
        {
            var command = new AVCommand(relativeUri: "rtm/transient_group/onlines?gid=" + chatroomId, method: "GET",
                sessionToken: null,
                headers: null,
                data: null);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
               {
                   var result = t.Result.Item2;
                   if (result.ContainsKey("result"))
                   {
                       return int.Parse(result["result"].ToString());
                   }
                   return -1;
               });
        }
        #endregion
        #endregion

        #region mark as read

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="message"></param>
        /// <param name="readAt"></param>
        /// <returns></returns>
        public Task ReadAsync(AVIMConversation conversation, IAVIMMessage message = null, DateTime? readAt = null)
        {
            var convRead = new ReadCommand.ConvRead()
            {
                ConvId = conversation.ConversationId,
            };

            if (message != null)
            {
                convRead.MessageId = message.Id;
                convRead.Timestamp = message.ServerTimestamp;
            }

            if (readAt != null && readAt.Value != DateTime.MinValue)
            {
                convRead.Timestamp = readAt.Value.ToUnixTimeStamp();
            }

            var readCmd = new ReadCommand().Conv(convRead).PeerId(this.ClientId);

            this.RunCommandAsync(readCmd);

            return Task.FromResult(true);
        }

        /// <summary>
        /// mark the conversation as read with conversation id.
        /// </summary>
        /// <param name="conversationId">conversation id</param>
        /// <returns></returns>
        public Task ReadAsync(string conversationId)
        {
            var conv = AVIMConversation.CreateWithoutData(conversationId, this);
            return this.ReadAsync(conv);
        }

        /// <summary>
        /// mark all conversations as read.
        /// </summary>
        /// <returns></returns>
        public Task ReadAllAsync()
        {
            var cids = ConversationUnreadListener.FindAllConvIds();
            var readCmd = new ReadCommand().ConvIds(cids).PeerId(this.ClientId);
            return this.RunCommandAsync(readCmd);
        }
        #endregion

        #region recall & modify

        /// <summary>
        /// Recalls the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="message">Message.</param>
        public Task<AVIMRecalledMessage> RecallAsync(IAVIMMessage message)
        {
            var tcs = new TaskCompletionSource<AVIMRecalledMessage>();
            var patchCmd = new PatchCommand().Recall(message);
            RunCommandAsync(patchCmd)
                .OnSuccess(t => {
                    var recalledMsg = new AVIMRecalledMessage();
                    AVIMMessage.CopyMetaData(message, recalledMsg);
                    tcs.SetResult(recalledMsg);
                });
            return tcs.Task;
        }

        /// <summary>
        /// Modifies the aysnc.
        /// </summary>
        /// <returns>The aysnc.</returns>
        /// <param name="oldMessage">要修改的消息对象</param>
        /// <param name="newMessage">新的消息对象</param>
        public Task<IAVIMMessage> UpdateAsync(IAVIMMessage oldMessage, IAVIMMessage newMessage)
        {
            var tcs = new TaskCompletionSource<IAVIMMessage>();
            var patchCmd = new PatchCommand().Modify(oldMessage, newMessage);
            this.RunCommandAsync(patchCmd)
                .OnSuccess(t => {
                    // 从旧消息对象中拷贝数据
                    AVIMMessage.CopyMetaData(oldMessage, newMessage);
                    // 获取更新时间戳
                    var response = t.Result.Item2;
                    if (response.TryGetValue("lastPatchTime", out object updatedAtObj) && 
                        long.TryParse(updatedAtObj.ToString(), out long updatedAt)) {
                        newMessage.UpdatedAt = updatedAt;
                    }
                    tcs.SetResult(newMessage);
                });
            return tcs.Task;
        }

        internal EventHandler<AVIMMessagePatchEventArgs> m_OnMessageRecalled;
        /// <summary>
        /// Occurs when on message recalled.
        /// </summary>
        public event EventHandler<AVIMMessagePatchEventArgs> OnMessageRecalled
        {
            add
            {
                this.m_OnMessageRecalled += value;
            }
            remove
            {
                this.m_OnMessageRecalled -= value;
            }
        }
        internal EventHandler<AVIMMessagePatchEventArgs> m_OnMessageUpdated;
        /// <summary>
        /// Occurs when on message modified.
        /// </summary>
        public event EventHandler<AVIMMessagePatchEventArgs> OnMessageUpdated
        {
            add
            {
                this.m_OnMessageUpdated += value;
            }
            remove
            {
                this.m_OnMessageUpdated -= value;
            }
        }

        #endregion

        #region log out
        /// <summary>
        /// 退出登录或者切换账号
        /// </summary>
        /// <returns></returns>
        public Task CloseAsync()
        {
            var cmd = new SessionCommand().Option("close");
            return this.RunCommandAsync(cmd).ContinueWith(t =>
            {
                m_OnSessionClosed(this, null);
            });
        }
        #endregion

        /// <summary>
        /// Run command async.
        /// </summary>
        /// <returns>The command async.</returns>
        /// <param name="command">Command.</param>
        public Task<Tuple<int, IDictionary<string, object>>> RunCommandAsync(AVIMCommand command)
        {
            command.PeerId(this.ClientId);
            return this.LinkedRealtime.RunCommandAsync(command);
        }

        /// <summary>
        /// Run command.
        /// </summary>
        /// <param name="command">Command.</param>
        public void RunCommand(AVIMCommand command)
        {
            command.PeerId(this.ClientId);
            this.LinkedRealtime.RunCommand(command);
        }
    }

    /// <summary>
    /// AVIMClient extensions.
    /// </summary>
    public static class AVIMClientExtensions
    {
        /// <summary>
        /// Create conversation async.
        /// </summary>
        /// <returns>The conversation async.</returns>
        /// <param name="client">Client.</param>
        /// <param name="members">Members.</param>
        public static Task<AVIMConversation> CreateConversationAsync(this AVIMClient client, IEnumerable<string> members)
        {
            return client.CreateConversationAsync(members: members);
        }

        public static Task<AVIMConversation> CreateConversationAsync(this AVIMClient client, IEnumerable<string> members, string conversationName)
        {
            return client.CreateConversationAsync(members: members, name: conversationName);
        }

        /// <summary>
        /// Get conversation.
        /// </summary>
        /// <returns>The conversation.</returns>
        /// <param name="client">Client.</param>
        /// <param name="conversationId">Conversation identifier.</param>
        public static AVIMConversation GetConversation(this AVIMClient client, string conversationId)
        {
            return AVIMConversation.CreateWithoutData(conversationId, client);
        }

        /// <summary>
        /// Join conversation async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="client">Client.</param>
        /// <param name="conversationId">Conversation identifier.</param>
        public static Task JoinAsync(this AVIMClient client, string conversationId)
        {
            var conversation = client.GetConversation(conversationId);
            return client.JoinAsync(conversation);
        }

        /// <summary>
        /// Leave conversation async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="client">Client.</param>
        /// <param name="conversationId">Conversation identifier.</param>
        public static Task LeaveAsync(this AVIMClient client, string conversationId)
        {
            var conversation = client.GetConversation(conversationId);
            return client.LeaveAsync(conversation);
        }

        /// <summary>
        /// Query messages.
        /// </summary>
        /// <returns>The message async.</returns>
        /// <param name="client">Client.</param>
        /// <param name="conversation">Conversation.</param>
        /// <param name="beforeMessageId">Before message identifier.</param>
        /// <param name="afterMessageId">After message identifier.</param>
        /// <param name="beforeTimeStampPoint">Before time stamp point.</param>
        /// <param name="afterTimeStampPoint">After time stamp point.</param>
        /// <param name="direction">Direction.</param>
        /// <param name="limit">Limit.</param>
        public static Task<IEnumerable<IAVIMMessage>> QueryMessageAsync(this AVIMClient client,
                                                                        AVIMConversation conversation,
                                                                        string beforeMessageId = null,
                                                                        string afterMessageId = null,
                                                                        DateTime? beforeTimeStampPoint = null,
                                                                        DateTime? afterTimeStampPoint = null,
                                                                        int direction = 1,
                                                                        int limit = 20)
        {
            return client.QueryMessageAsync<IAVIMMessage>(conversation,
                                                         beforeMessageId,
                                                         afterMessageId,
                                                         beforeTimeStampPoint,
                                                         afterTimeStampPoint,
                                                         direction,
                                                          limit);
        }

        /// <summary>
        /// Get the chat room query.
        /// </summary>
        /// <returns>The chat room query.</returns>
        /// <param name="client">Client.</param>
        public static AVIMConversationQuery GetChatRoomQuery(this AVIMClient client)
        {
            return client.GetQuery().WhereEqualTo("tr", true);
        }
    }
}
