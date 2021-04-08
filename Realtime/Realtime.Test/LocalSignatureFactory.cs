using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using LeanCloud.Realtime;
using LeanCloud;

namespace Realtime.Test {
    public class LocalSignatureFactory : ILCIMSignatureFactory {
        const string MasterKey = "pyvbNSh5jXsuFQ3C8EgnIdhw";

        public Task<LCIMSignature> CreateConnectSignature(string clientId) {
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string nonce = NewNonce();
            string signature = GenerateSignature(LCInternalApplication.AppId, clientId, string.Empty, timestamp.ToString(), nonce);
            return Task.FromResult(new LCIMSignature {
                Signature = signature,
                Timestamp = timestamp,
                Nonce = nonce
            });
        }

        public Task<LCIMSignature> CreateStartConversationSignature(string clientId, IEnumerable<string> memberIds) {
            string sortedMemberIds = string.Empty;
            if (memberIds != null) {
                List<string> sortedMemberList = memberIds.ToList();
                sortedMemberList.Sort();
                sortedMemberIds = string.Join(":", sortedMemberList);
            }
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string nonce = NewNonce();
            string signature = GenerateSignature(LCInternalApplication.AppId, clientId, sortedMemberIds, timestamp.ToString(), nonce);
            return Task.FromResult(new LCIMSignature {
                Signature = signature,
                Timestamp = timestamp,
                Nonce = nonce
            });
        }

        public Task<LCIMSignature> CreateConversationSignature(string conversationId, string clientId, IEnumerable<string> memberIds, string action) {
            string sortedMemberIds = string.Empty;
            if (memberIds != null) {
                List<string> sortedMemberList = memberIds.ToList();
                sortedMemberList.Sort();
                sortedMemberIds = string.Join(":", sortedMemberList);
            }
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string nonce = NewNonce();
            string signature = GenerateSignature(LCInternalApplication.AppId, clientId, conversationId, sortedMemberIds, timestamp.ToString(), nonce, action);
            return Task.FromResult(new LCIMSignature {
                Signature = signature,
                Timestamp = timestamp,
                Nonce = nonce
            });
        }

        public Task<LCIMSignature> CreateBlacklistSignature(string conversationId, string clientId, IEnumerable<string> memberIds, string action) {
            string sortedMemberIds = string.Empty;
            if (memberIds != null) {
                List<string> sortedMemberList = memberIds.ToList();
                sortedMemberList.Sort();
                sortedMemberIds = string.Join(":", sortedMemberList);
            }
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            string nonce = NewNonce();
            string signature = GenerateSignature(LCInternalApplication.AppId, clientId, conversationId, sortedMemberIds, timestamp.ToString(), nonce, action);
            return Task.FromResult(new LCIMSignature {
                Signature = signature,
                Timestamp = timestamp,
                Nonce = nonce
            });
        }

        private static string SignSHA1(string key, string text) {
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
            string signature = BitConverter.ToString(bytes).Replace("-", string.Empty);
            return signature;
        }

        private static string NewNonce() {
            byte[] bytes = new byte[10];
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create()) {
                generator.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateSignature(params string[] args) {
            string text = string.Join(":", args);
            string signature = SignSHA1(MasterKey, text);
            return signature;
        }
    }
}
