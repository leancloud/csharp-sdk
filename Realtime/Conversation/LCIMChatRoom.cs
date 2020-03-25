using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime {
    public class LCIMChatRoom : LCIMConversation {
        public LCIMChatRoom(LCIMClient client) : base(client) {
        }

        public async Task<int> GetOnlineMembersCount() {
            return await GetMembersCount();
        }

        public async Task<List<string>> GetOnlineMembers(int limit = 50) {
            ConvCommand conv = new ConvCommand {
                Cid = Id,
                Limit = limit
            };
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Members);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            List<string> memberList = response.ConvMessage.M.ToList();
            return memberList;
        }
    }
}
