using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeanCloud.Realtime.Internal;

namespace LeanCloud.Realtime
{
    internal class ConversationUnreadListener : IAVIMListener
    {
        internal class UnreadConversationNotice : IEqualityComparer<UnreadConversationNotice>
        {
            internal readonly object mutex = new object();
            internal IAVIMMessage LastUnreadMessage { get; set; }
            internal string ConvId { get; set; }
            internal int UnreadCount { get; set; }

            public bool Equals(UnreadConversationNotice x, UnreadConversationNotice y)
            {
                return x.ConvId == y.ConvId;
            }

            public int GetHashCode(UnreadConversationNotice obj)
            {
                return obj.ConvId.GetHashCode();
            }

            internal void AutomicIncrement()
            {
                lock (mutex)
                {
                    UnreadCount++;
                }
            }
        }
        internal static readonly object sMutex = new object();
        internal static long NotifTime;
        internal static HashSet<UnreadConversationNotice> UnreadConversations;
        static ConversationUnreadListener()
        {
            UnreadConversations = new HashSet<UnreadConversationNotice>(new UnreadConversationNotice());
            NotifTime = DateTime.Now.ToUnixTimeStamp();
        }

        internal static void UpdateNotice(IAVIMMessage message)
        {
            lock (sMutex)
            {
                var convValidators = UnreadConversations.Where(c => c.ConvId == message.ConversationId);
                if (convValidators != null)
                {
                    if (convValidators.Count() > 0)
                    {
                        var currentNotice = convValidators.FirstOrDefault();
                        currentNotice.AutomicIncrement();
                        currentNotice.LastUnreadMessage = message;
                    }
                    else
                    {
                        var currentThread = new UnreadConversationNotice();
                        currentThread.ConvId = message.ConversationId;
                        currentThread.LastUnreadMessage = message;
                        currentThread.AutomicIncrement();
                        UnreadConversations.Add(currentThread);
                    }
                }
            }
        }
        internal static void ClearUnread(string convId)
        {
            UnreadConversations.Remove(Get(convId));
        }
        internal static IEnumerable<string> FindAllConvIds()
        {
            lock (sMutex)
            {
                return ConversationUnreadListener.UnreadConversations.Select(c => c.ConvId);
            }
        }

        internal static UnreadConversationNotice Get(string convId)
        {
            lock (sMutex)
            {
                var unreadValidator = ConversationUnreadListener.UnreadConversations.Where(c => c.ConvId == convId);
                if (unreadValidator != null)
                {
                    if (unreadValidator.Count() > 0)
                    {
                        var notice = unreadValidator.FirstOrDefault();
                        return notice;
                    }
                }
                return null;
            }
        }

        public void OnNoticeReceived(AVIMNotice notice)
        {
            lock (sMutex)
            {
                if (notice.RawData.ContainsKey("convs"))
                {
                    var unreadRawData = notice.RawData["convs"] as List<object>;
                    if (notice.RawData.ContainsKey("notifTime"))
                    {
                        long.TryParse(notice.RawData["notifTime"].ToString(), out NotifTime);
                    }
                    foreach (var data in unreadRawData)
                    {
                        var dataMap = data as IDictionary<string, object>;
                        if (dataMap != null)
                        {
                            var convId = dataMap["cid"].ToString();
                            var ucn = Get(convId);
                            if (ucn == null) ucn = new UnreadConversationNotice();

                            ucn.ConvId = convId;
                            var unreadCount = 0;
                            Int32.TryParse(dataMap["unread"].ToString(), out unreadCount);
                            ucn.UnreadCount = unreadCount;

                            #region restore last message for the conversation
                            if (dataMap.ContainsKey("data"))
                            {
                                var msgStr = dataMap["data"].ToString();
                                var messageObj = AVRealtime.FreeStyleMessageClassingController.Instantiate(msgStr, dataMap);
                                ucn.LastUnreadMessage = messageObj;
                            }

                            UnreadConversations.Add(ucn);
                            #endregion
                        }
                    }
                }
            }
        }

        public bool ProtocolHook(AVIMNotice notice)
        {
            return notice.CommandName == "unread";
        }
    }
}
