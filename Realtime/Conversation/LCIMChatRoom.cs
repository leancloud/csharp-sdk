using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 聊天室
    /// </summary>
    public class LCIMChatRoom : LCIMConversation {
        public LCIMChatRoom(LCIMClient client) :
            base(client) {
        }

        /// <summary>
        /// 获取在线用户数量
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetOnlineMembersCount() {
            return await GetMembersCount();
        }

        /// <summary>
        /// 获取在线用户
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<string>> GetOnlineMembers(int limit = 50) {
            return await Client.ConversationController.GetOnlineMembers(Id, limit);
        }

        public override Task<LCIMPartiallySuccessResult> AddMembers(IEnumerable<string> clientIds) {
            throw new Exception("Add members is not allowed in chat room.");
        }
    }
}
