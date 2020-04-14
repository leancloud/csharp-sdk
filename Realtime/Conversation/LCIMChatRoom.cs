using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    public class LCIMChatRoom : LCIMConversation {
        public LCIMChatRoom(LCIMClient client) :
            base(client) {
        }

        public async Task<int> GetOnlineMembersCount() {
            return await GetMembersCount();
        }

        public async Task<ReadOnlyCollection<string>> GetOnlineMembers(int limit = 50) {
            return await Client.ConversationController.GetOnlineMembers(Id, limit);
        }
    }
}
