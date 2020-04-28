using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    public interface ILCIMSignatureFactory {
        /// <summary>
        /// 登录签名
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        Task<LCIMSignature> CreateConnectSignature(string clientId);

        /// <summary>
        /// 创建开启对话签名
        /// </summary>
        /// <returns></returns>
        Task<LCIMSignature> CreateStartConversationSignature(string clientId, IEnumerable<string> memberIds);

        /// <summary>
        /// 创建会话相关签名
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="clientId"></param>
        /// <param name="memberIds"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        Task<LCIMSignature> CreateConversationSignature(string conversationId, string clientId, IEnumerable<string> memberIds, string action);

        /// <summary>
        /// 创建黑名单相关签名
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="clientId"></param>
        /// <param name="memberIds"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        Task<LCIMSignature> CreateBlacklistSignature(string conversationId, string clientId, IEnumerable<string> memberIds, string action);
    }
}
