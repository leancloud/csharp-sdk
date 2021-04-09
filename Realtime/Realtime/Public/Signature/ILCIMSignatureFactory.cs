using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    public interface ILCIMSignatureFactory {
        Task<LCIMSignature> CreateConnectSignature(string clientId);

        Task<LCIMSignature> CreateStartConversationSignature(string clientId, IEnumerable<string> memberIds);

        Task<LCIMSignature> CreateConversationSignature(string conversationId, string clientId, IEnumerable<string> memberIds, string action);

        Task<LCIMSignature> CreateBlacklistSignature(string conversationId, string clientId, IEnumerable<string> memberIds, string action);
    }
}
