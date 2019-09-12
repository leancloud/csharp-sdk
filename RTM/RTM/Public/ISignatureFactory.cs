using LeanCloud;
using LeanCloud.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LeanCloud.Realtime
{
    /// <summary>
    /// 对话操作的签名类型，比如讲一个 client id 加入到对话中
    /// <see cref="https://leancloud.cn/docs/realtime_v2.html#群组功能的签名"/>
    /// </summary>
    public enum ConversationSignatureAction
    {
        /// <summary>
        /// add 加入对话和邀请对方加入对话
        /// </summary>
        Add,
        /// <summary>
        /// remove 当前 client Id 离开对话和将其他人踢出对话
        /// </summary>
        Remove
    }

    /// <summary>
    /// <see cref="https://leancloud.cn/docs/realtime_v2.html#群组功能的签名"/>
    /// </summary>
    public interface ISignatureFactory
    {

        /// <summary>
        /// 构建登录签名
        /// </summary>
        /// <param name="clientId">需要登录到云端服务器的 client Id</param>
        /// <returns></returns>
        Task<AVIMSignature> CreateConnectSignature(string clientId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="targetIds"></param>
        /// <returns></returns>
        Task<AVIMSignature> CreateStartConversationSignature(string clientId, IEnumerable<string> targetIds);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="clientId"></param>
        /// <param name="targetIds"></param>
        /// <param name="action">需要签名的操作</param>
        /// <returns></returns>
        Task<AVIMSignature> CreateConversationSignature(string conversationId, string clientId, IEnumerable<string> targetIds, ConversationSignatureAction action);
    }

    internal class DefaulSiganatureFactory : ISignatureFactory
    {
        Task<AVIMSignature> ISignatureFactory.CreateConnectSignature(string clientId)
        {
            return Task.FromResult<AVIMSignature>(null);
        }

        Task<AVIMSignature> ISignatureFactory.CreateConversationSignature(string conversationId, string clientId, IEnumerable<string> targetIds, ConversationSignatureAction action)
        {
            return Task.FromResult<AVIMSignature>(null);
        }

        Task<AVIMSignature> ISignatureFactory.CreateStartConversationSignature(string clientId, IEnumerable<string> targetIds)
        {
            return Task.FromResult<AVIMSignature>(null);
        }
    }

    public class LeanEngineSignatureFactory : ISignatureFactory
    {
        public Task<AVIMSignature> CreateConnectSignature(string clientId)
        {
            var data = new Dictionary<string, object>();
            data.Add("client_id", clientId);
            return AVCloud.CallFunctionAsync<IDictionary<string, object>>("connect", data).OnSuccess(_ =>
             {
                 var jsonData = _.Result;
                 var s = jsonData["signature"].ToString();
                 var n = jsonData["nonce"].ToString();
                 var t = long.Parse(jsonData["timestamp"].ToString());
                 var signature = new AVIMSignature(s, t, n);
                 return signature;
             });
        }

        public Task<AVIMSignature> CreateStartConversationSignature(string clientId, IEnumerable<string> targetIds)
        {
            var data = new Dictionary<string, object>();
            data.Add("client_id", clientId);
            data.Add("members", targetIds.ToList());
            return AVCloud.CallFunctionAsync<IDictionary<string, object>>("startConversation", data).OnSuccess(_ =>
            {
                var jsonData = _.Result;
                var s = jsonData["signature"].ToString();
                var n = jsonData["nonce"].ToString();
                var t = long.Parse(jsonData["timestamp"].ToString());
                var signature = new AVIMSignature(s, t, n);
                return signature;
            });
        }

        public Task<AVIMSignature> CreateConversationSignature(string conversationId, string clientId, IEnumerable<string> targetIds, ConversationSignatureAction action)
        {
            var actionList = new string[] { "invite", "kick" };
            var data = new Dictionary<string, object>();
            data.Add("client_id", clientId);
            data.Add("conv_id", conversationId);
            data.Add("members", targetIds.ToList());
            data.Add("action", actionList[(int)action]);
            return AVCloud.CallFunctionAsync<IDictionary<string, object>>("oprateConversation", data).OnSuccess(_ =>
            {
                var jsonData = _.Result;
                var s = jsonData["signature"].ToString();
                var n = jsonData["nonce"].ToString();
                var t = long.Parse(jsonData["timestamp"].ToString());
                var signature = new AVIMSignature(s, t, n);
                return signature;
            });
        }

    }
}
