using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMChatRoom is a local representation of chatroom in LeanCloud.
    /// </summary>
    public class LCIMChatRoom : LCIMConversation {
        public LCIMChatRoom(LCIMClient client) :
            base(client) {
        }

        public async Task<int> GetOnlineMembersCount() {
            return await GetMembersCount();
        }

        /// <summary>
        /// Gets online members.
        /// </summary>
        /// <param name="limit">Query limit, defaults to 50.</param>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<string>> GetOnlineMembers(int limit = 50) {
            return await Client.ConversationController.GetOnlineMembers(Id, limit);
        }

        /// <summary>
        /// Adds members to this conversation.
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public override Task<LCIMPartiallySuccessResult> AddMembers(IEnumerable<string> clientIds) {
            throw new Exception("Add members is not allowed in chat room.");
        }

        /// <summary>
        /// Flags the read status of this conversation.
        /// But it is an no-op in LCIMChatRoom.
        /// </summary>
        /// <returns></returns>
        public override Task Read() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fetchs the recipt timestamps of this conversation.
        /// But it's nothing to do in LCIMChatRoom.
        /// </summary>
        /// <returns></returns>
        public override Task FetchReciptTimestamps() {
            return Task.CompletedTask;
        }
    }
}
