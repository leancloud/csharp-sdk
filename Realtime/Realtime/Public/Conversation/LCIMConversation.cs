using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMConversation is a local representation of general conversation
    /// in LeanCloud.
    /// </summary>
    public class LCIMConversation {
        /// <summary>
        /// The ID of this conversation
        /// </summary>
        public string Id {
            get; internal set;
        }

        /// <summary>
        /// Indicates whether this conversation is normal and unique. The uniqueness is based on the members when creating. 
        /// </summary>
        public bool Unique {
            get; internal set;
        }

        /// <summary>
        /// If this conversation is unique, then it will have a unique ID.
        /// </summary>
        public string UniqueId {
            get; internal set;
        }

        /// <summary>
        /// The name of this conversation.
        /// </summary>
        public string Name {
            get {
                return this["name"] as string;
            }
            internal set {
                this["name"] = value;
            }
        }

        /// <summary>
        /// The creator of this conversation.
        /// </summary>
        public string CreatorId {
            get; set;
        }

        /// <summary>
        /// The members of this conversation.
        /// </summary>
        public ReadOnlyCollection<string> MemberIds {
            get {
                return new ReadOnlyCollection<string>(ids.ToList());
            }
        }

        /// <summary>
        /// Muted members of this conversation.
        /// </summary>
        public ReadOnlyCollection<string> MutedMemberIds {
            get {
                return new ReadOnlyCollection<string>(mutedIds.ToList());
            }
        }

        /// <summary>
        /// The count of the unread messages.
        /// </summary>
        public int Unread {
            get; internal set;
        }

        /// <summary>
        /// The last message in this conversation.
        /// </summary>
        public LCIMMessage LastMessage {
            get; internal set;
        }

        /// <summary>
        /// The created date of this conversation. 
        /// </summary>
        public DateTime CreatedAt {
            get; internal set;
        }

        /// <summary>
        /// The last updated date of this conversation.
        /// </summary>
        public DateTime UpdatedAt {
            get; internal set;
        }

        /// <summary>
        /// The last timestamp of the delivered message.
        /// </summary>
        public long LastDeliveredTimestamp {
            get; internal set;
        }

        /// <summary>
        /// The last date of the delivered message.
        /// </summary>
        public DateTime LastDeliveredAt {
            get {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(LastDeliveredTimestamp);
                return dateTimeOffset.DateTime;
            }
        }

        /// <summary>
        /// The last timestamp of the message which has been read by other clients.
        /// </summary>
        public long LastReadTimestamp {
            get; internal set;
        }

        /// <summary>
        /// The last date of the message which has been read by other clients. 
        /// </summary>
        public DateTime LastReadAt {
            get {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(LastReadTimestamp);
                return dateTimeOffset.DateTime;
            }
        }

        /// <summary>
        /// Custom attributes.
        /// </summary>
        /// <param name="key">Custom attribute name.</param>
        /// <returns></returns>
        public object this[string key] {
            get {
                if (customProperties.TryGetValue(key, out object val)) {
                    return val;
                }
                return null;
            }
            set {
                customProperties[key] = value;
            }
        }

        /// <summary>
        /// Indicates whether offline notifications about this conversation has been muted.
        /// </summary>
        public bool IsMute {
            get; private set;
        }

        protected LCIMClient Client {
            get; private set;
        }

        private readonly Dictionary<string, object> customProperties;

        internal HashSet<string> ids;

        internal HashSet<string> mutedIds;

        internal LCIMConversation(LCIMClient client) {
            Client = client;
            customProperties = new Dictionary<string, object>();
            ids = new HashSet<string>();
            mutedIds = new HashSet<string>();
            customProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// The count of members of this conversation.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetMembersCount() {
            return await Client.ConversationController.GetMembersCount(Id);
        }

        /// <summary>
        /// Mark the last message of this conversation as read.
        /// </summary>
        /// <returns></returns>
        public virtual async Task Read() {
            if (Unread == 0) {
                return;
            }
            await Client.MessageController.Read(Id, LastMessage);
            Unread = 0;
        }

        /// <summary>
        /// Update attributes of this conversation.
        /// </summary>
        /// <param name="attributes">Attributes to update.</param>
        /// <returns></returns>
        public async Task UpdateInfo(Dictionary<string, object> attributes) {
            if (attributes == null || attributes.Count == 0) {
                throw new ArgumentNullException(nameof(attributes));
            }
            Dictionary<string, object> updatedAttr = await Client.ConversationController.UpdateInfo(Id, attributes);
            if (updatedAttr != null) {
                MergeInfo(updatedAttr);
            }
        }

        /// <summary>
        /// Adds members to this conversation.
        /// </summary>
        /// <param name="clientIds">Member list.</param>
        /// <returns></returns>
        public virtual async Task<LCIMPartiallySuccessResult> AddMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            LCIMPartiallySuccessResult result = await Client.ConversationController.AddMembers(Id, clientIds);
            ids.UnionWith(result.SuccessfulClientIdList);
            return result;
        }

        /// <summary>
        /// Removes members from this conversation.
        /// </summary>
        /// <param name="removeIds">Member list.</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> RemoveMembers(IEnumerable<string> removeIds) {
            if (removeIds == null || removeIds.Count() == 0) {
                throw new ArgumentNullException(nameof(removeIds));
            }
            LCIMPartiallySuccessResult result = await Client.ConversationController.RemoveMembers(Id, removeIds);
            ids.RemoveWhere(id => result.SuccessfulClientIdList.Contains(id));
            return result;
        }

        /// <summary>
        /// Joins this conversation.
        /// </summary>
        /// <returns></returns>
        public async Task Join() {
            LCIMPartiallySuccessResult result = await Client.ConversationController.AddMembers(Id,
                new string[] { Client.Id });
            if (result.IsSuccess) {
                ids.UnionWith(result.SuccessfulClientIdList);
            } else {
                LCIMOperationFailure error = result.FailureList[0];
                throw new LCException(error.Code, error.Reason);
            }
        }

        /// <summary>
        /// Leaves this conversation.
        /// </summary>
        /// <returns></returns>
        public async Task Quit() {
            LCIMPartiallySuccessResult result = await RemoveMembers(new string[] { Client.Id });
            if (!result.IsSuccess) {
                LCIMOperationFailure error = result.FailureList[0];
                throw new LCException(error.Code, error.Reason);
            }
        }



        /// <summary>
        /// Sends a message in this conversation.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options of sending message.</param>
        /// <returns></returns>
        public async Task<LCIMMessage> Send(LCIMMessage message,
            LCIMMessageSendOptions options = null) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (options == null) {
                options = LCIMMessageSendOptions.Default;
            }
            await message.PrepareSend();
            await Client.MessageController.Send(Id, message, options);
            LastMessage = message;
            return message;
        }

        /// <summary>
        /// Turns off the offline notifications of this conversation.
        /// </summary>
        /// <returns></returns>
        public async Task Mute() {
            await Client.ConversationController.Mute(Id);
            IsMute = true;
        }

        /// <summary>
        /// Turns on the offline notifications of this conversation. 
        /// </summary>
        /// <returns></returns>
        public async Task Unmute() {
            await Client.ConversationController.Unmute(Id);
            IsMute = false;
        }

        /// <summary>
        /// Mutes members of this conversation.
        /// </summary>
        /// <param name="clientIds">Member list.</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> MuteMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            LCIMPartiallySuccessResult result = await Client.ConversationController.MuteMembers(Id, clientIds);
            if (result.SuccessfulClientIdList != null) {
                mutedIds.UnionWith(result.SuccessfulClientIdList);
            }
            return result;
        }

        /// <summary>
        /// Unmutes members of this conversation.
        /// </summary>
        /// <param name="clientIdList">Member list.</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> UnmuteMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            LCIMPartiallySuccessResult result = await Client.ConversationController.UnmuteMembers(Id, clientIds);
            if (result.SuccessfulClientIdList != null) {
                mutedIds.RemoveWhere(id => result.SuccessfulClientIdList.Contains(id));
            }
            return result;
        }

        /// <summary>
        /// Adds members to the blocklist of this conversation.
        /// </summary>
        /// <param name="clientIds">Member list.</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> BlockMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.BlockMembers(Id, clientIds);
        }

        /// <summary>
        /// Removes members from the blocklist of this conversation. 
        /// </summary>
        /// <param name="clientIds">Member list.</param>
        /// <returns></returns>
        public async Task<LCIMPartiallySuccessResult> UnblockMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            return await Client.ConversationController.UnblockMembers(Id, clientIds);
        }

        /// <summary>
        /// Recalls a sent message.
        /// </summary>
        /// <param name="message">The message to recall.</param>
        /// <returns></returns>
        public async Task RecallMessage(LCIMMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            await Client.MessageController.RecallMessage(Id, message);
        }

        /// <summary>
        /// Updates a sent message.
        /// </summary>
        /// <param name="oldMessage">The message to update.</param>
        /// <param name="newMessage">The updated message.</param>
        /// <returns></returns>
        public async Task UpdateMessage(LCIMMessage oldMessage, LCIMMessage newMessage) {
            if (oldMessage == null) {
                throw new ArgumentNullException(nameof(oldMessage));
            }
            if (newMessage == null) {
                throw new ArgumentNullException(nameof(newMessage));
            }
            await Client.MessageController.UpdateMessage(Id, oldMessage, newMessage);
        }

        /// <summary>
        /// Updates the role of a member of this conversation.
        /// </summary>
        /// <param name="memberId">The member to update.</param>
        /// <param name="role">The new role of the member.</param>
        /// <returns></returns>
        public async Task UpdateMemberRole(string memberId, string role) {
            if (string.IsNullOrEmpty(memberId)) {
                throw new ArgumentNullException(nameof(memberId));
            }
            if (role != LCIMConversationMemberInfo.Manager && role != LCIMConversationMemberInfo.Member) {
                throw new ArgumentException("role MUST be Manager Or Memebr");
            }
            await Client.ConversationController.UpdateMemberRole(Id, memberId, role);
        }

        /// <summary>
        /// Gets all member roles.
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMConversationMemberInfo>> GetAllMemberInfo() {
            return await Client.ConversationController.GetAllMemberInfo(Id);
        }

        /// <summary>
        /// Gets the role of a specific member.
        /// </summary>
        /// <param name="memberId">The member to query.</param>
        /// <returns></returns>
        public async Task<LCIMConversationMemberInfo> GetMemberInfo(string memberId) {
            if (string.IsNullOrEmpty(memberId)) {
                throw new ArgumentNullException(nameof(memberId));
            }
            ReadOnlyCollection<LCIMConversationMemberInfo> members = await GetAllMemberInfo();
            foreach (LCIMConversationMemberInfo member in members) {
                if (member.MemberId == memberId) {
                    return member;
                }
            }
            return null;
        }

        /// <summary>
        /// Queries muted members.
        /// </summary>
        /// <param name="limit">Limits the number of returned results.</param>
        /// <param name="next">Can be used for pagination with the limit parameter.</param>
        /// <returns></returns>
        public async Task<LCIMPageResult> QueryMutedMembers(int limit = 10,
            string next = null) {
            return await Client.ConversationController.QueryMutedMembers(Id, limit, next);
        }

        /// <summary>
        /// Queries blocked members.
        /// </summary>
        /// <param name="limit">Limits the number of returned results.</param>
        /// <param name="next">Can be used for pagination with the limit parameter.</param>
        /// <returns></returns>
        public async Task<LCIMPageResult> QueryBlockedMembers(int limit = 10,
            string next = null) {
            return await Client.ConversationController.QueryBlockedMembers(Id, limit, next);
        }

        /// <summary>
        /// Retrieves messages.
        /// </summary>
        /// <param name="start">Start message ID.</param>
        /// <param name="end">End message ID.</param>
        /// <param name="direction">Query direction (defaults to NewToOld).</param>
        /// <param name="limit">Limits the number of returned results. Its default value is 100.</param>
        /// <param name="messageType">The message type to query. The default value is 0 (text message).</param>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<LCIMMessage>> QueryMessages(LCIMMessageQueryEndpoint start = null,
            LCIMMessageQueryEndpoint end = null,
            LCIMMessageQueryDirection direction = LCIMMessageQueryDirection.NewToOld,
            int limit = 20,
            int messageType = 0) {
            return await Client.MessageController.QueryMessages(Id, start, end, direction, limit, messageType);
        }

        /// <summary>
        /// Fetches receipt timestamp.
        /// </summary>
        /// <returns></returns>
        public virtual async Task FetchReciptTimestamps() {
            await Client.ConversationController.FetchReciptTimestamp(Id);
        }

        /// <summary>
        /// Fetch conversation from server.
        /// </summary>
        /// <returns></returns>
        public async Task<LCIMConversation> Fetch() {
            LCIMConversationQuery query = new LCIMConversationQuery(Client);
            query.WhereEqualTo("objectId", Id);
            await query.Find();
            return this;
        }

        internal static bool IsTemporayConversation(string convId) {
            return convId.StartsWith("_tmp:");
        }

        internal void MergeFrom(Dictionary<string, object> conv) {
            if (conv.TryGetValue("objectId", out object idObj)) {
                Id = idObj as string;
            }
            if (conv.TryGetValue("name", out object nameObj)) {
                Name = nameObj as string;
            }
            if (conv.TryGetValue("unique", out object uniqueObj)) {
                Unique = (bool)uniqueObj;
            }
            if (conv.TryGetValue("uniqueId", out object uniqueIdObj)) {
                UniqueId = uniqueIdObj as string;
            }
            if (conv.TryGetValue("createdAt", out object createdAtObj)) {
                CreatedAt = DateTime.Parse(createdAtObj.ToString());
            }
            if (conv.TryGetValue("updatedAt", out object updatedAtObj)) {
                UpdatedAt = DateTime.Parse(updatedAtObj.ToString());
            }
            if (conv.TryGetValue("c", out object co)) {
                CreatorId = co as string;
            }
            if (conv.TryGetValue("m", out object mo)) {
                IEnumerable<string> ids = (mo as IList<object>).Cast<string>();
                this.ids = new HashSet<string>(ids);
            }
            if (conv.TryGetValue("mu", out object muo)) {
                IEnumerable<string> ids = (muo as IList<object>).Cast<string>();
                mutedIds = new HashSet<string>(ids);
            }
            if (conv.TryGetValue("msg", out object msgo)) {
                if (conv.TryGetValue("bin", out object bino)) {
                    string msg = msgo as string;
                    bool bin = (bool)bino;
                    if (bin) {
                        byte[] bytes = Convert.FromBase64String(msg);
                        LastMessage = LCIMBinaryMessage.Deserialize(bytes);
                    } else {
                        LastMessage = LCIMTypedMessage.Deserialize(msg);
                    }
                }
                LastMessage.ConversationId = Id;
                if (conv.TryGetValue("msg_mid", out object msgId)) {
                    LastMessage.Id = msgId as string;
                }
                if (conv.TryGetValue("msg_from", out object msgFrom)) {
                    LastMessage.FromClientId = msgFrom as string;
                }
                if (conv.TryGetValue("msg_timestamp", out object timestamp)) {
                    LastMessage.SentTimestamp = (long)timestamp;
                }
            }
        }

        internal void MergeInfo(Dictionary<string, object> attr) {
            if (attr == null || attr.Count == 0) {
                return;
            }
            foreach (KeyValuePair<string, object> kv in attr) {
                customProperties[kv.Key] = kv.Value;
            }
        }
    }
}
