using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    /// <summary>
    /// Chatroom
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

        public override Task<LCIMPartiallySuccessResult> AddMembers(IEnumerable<string> clientIds) {
            throw new Exception("Add members is not allowed in chat room.");
        }
    }
}
