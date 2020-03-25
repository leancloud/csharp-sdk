using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal;
using LeanCloud.Storage.Internal.Codec;
using Google.Protobuf;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMConversationController : LCIMController {
        internal LCIMConversationController(LCIMClient client) : base(client) {

        }

        /// <summary>
        /// 创建对话
        /// </summary>
        /// <param name="members"></param>
        /// <param name="name"></param>
        /// <param name="transient"></param>
        /// <param name="unique"></param>
        /// <param name="temporary"></param>
        /// <param name="temporaryTtl"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        internal async Task<LCIMConversation> CreateConv(
            IEnumerable<string> members = null,
            string name = null,
            bool transient = false,
            bool unique = true,
            bool temporary = false,
            int temporaryTtl = 86400,
            Dictionary<string, object> properties = null) {
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Start);
            ConvCommand conv = new ConvCommand {
                Transient = transient,
                Unique = unique,
            };
            if (members != null) {
                conv.M.AddRange(members);
            }
            if (!string.IsNullOrEmpty(name)) {
                conv.N = name;
            }
            if (temporary) {
                conv.TempConv = temporary;
                conv.TempConvTTL = temporaryTtl;
            }
            if (properties != null) {
                conv.Attr = new JsonObjectMessage {
                    Data = JsonConvert.SerializeObject(LCEncoder.Encode(properties))
                };
            }
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = Client.SignatureFactory.CreateStartConversationSignature(Client.Id, members);
                conv.S = signature.Signature;
                conv.T = signature.Timestamp;
                conv.N = signature.Nonce;
            }
            request.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(request);
            string convId = response.ConvMessage.Cid;
            if (!Client.ConversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                if (transient) {
                    conversation = new LCIMChatRoom(Client);
                } else if (temporary) {
                    conversation = new LCIMTemporaryConversation(Client);
                } else if (properties != null && properties.ContainsKey("system")) {
                    conversation = new LCIMServiceConversation(Client);
                } else {
                    conversation = new LCIMConversation(Client);
                }
                Client.ConversationDict[convId] = conversation;
            }
            // 合并请求数据
            conversation.Name = name;
            conversation.MemberIdList = members?.ToList();
            // 合并服务端推送的数据
            conversation.MergeFrom(response.ConvMessage);
            return conversation;
        }

        internal async Task<int> GetMembersCount(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            GenericCommand command = Client.NewCommand(CommandType.Conv, OpType.Count);
            command.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(command);
            return response.ConvMessage.Count;
        }

        internal async Task Read(string convId, LCIMMessage message) {
            ReadCommand read = new ReadCommand();
            ReadTuple tuple = new ReadTuple {
                Cid = convId,
                Mid = message.Id,
                Timestamp = message.SentTimestamp
            };
            read.Convs.Add(tuple);
            GenericCommand request = Client.NewCommand(CommandType.Read, OpType.Open);
            request.ReadMessage = read;
            await Client.Connection.SendRequest(request);
        }

        internal async Task<Dictionary<string, object>> UpdateInfo(string convId, Dictionary<string, object> attributes) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            conv.Attr = new JsonObjectMessage {
                Data = JsonConvert.SerializeObject(attributes)
            };
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Update);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            JsonObjectMessage attr = response.ConvMessage.AttrModified;
            // 更新自定义属性
            if (attr != null) {
                Dictionary<string, object> updatedAttr = JsonConvert.DeserializeObject<Dictionary<string, object>>(attr.Data);
                return updatedAttr;
            }
            return null;
        }

        internal async Task<LCIMPartiallySuccessResult> AddMembers(string convId, IEnumerable<string> clientIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            conv.M.AddRange(clientIds);
            // 签名参数
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = Client.SignatureFactory.CreateConversationSignature(convId,
                    Client.Id,
                    clientIds,
                    LCIMSignatureAction.Invite);
                conv.S = signature.Signature;
                conv.T = signature.Timestamp;
                conv.N = signature.Nonce;
            }
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Add);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> errors = response.ConvMessage.FailedPids.ToList();
            return NewPartiallySuccessResult(allowedIds, errors);
        }

        internal async Task<LCIMPartiallySuccessResult> RemoveMembers(string convId, IEnumerable<string> removeIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            conv.M.AddRange(removeIds);
            // 签名参数
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = Client.SignatureFactory.CreateConversationSignature(convId,
                    Client.Id,
                    removeIds,
                    LCIMSignatureAction.Kick);
                conv.S = signature.Signature;
                conv.T = signature.Timestamp;
                conv.N = signature.Nonce;
            }
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Remove);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> errors = response.ConvMessage.FailedPids.ToList();
            return NewPartiallySuccessResult(allowedIds, errors);
        }

        internal async Task Mute(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Mute);
            request.ConvMessage = conv;
            await Client.Connection.SendRequest(request);
        }

        internal async Task Unmute(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Unmute);
            request.ConvMessage = conv;
            await Client.Connection.SendRequest(request);
        }

        internal async Task<LCIMPartiallySuccessResult> MuteMembers(string convId, IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.AddShutup);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.ConvMessage.AllowedPids, response.ConvMessage.FailedPids);
        }

        internal async Task<LCIMPartiallySuccessResult> UnmuteMembers(string convId, IEnumerable<string> clientIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.Remove);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.ConvMessage.AllowedPids, response.ConvMessage.FailedPids);
        }

        internal async Task<LCIMPartiallySuccessResult> BlockMembers(string convId, IEnumerable<string> clientIds) {
            BlacklistCommand blacklist = new BlacklistCommand {
                SrcCid = convId,
            };
            blacklist.ToPids.AddRange(clientIds);
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = Client.SignatureFactory.CreateBlacklistSignature(convId,
                    Client.Id,
                    clientIds,
                    LCIMSignatureAction.ConversationBlockClients);
                blacklist.S = signature.Signature;
                blacklist.T = signature.Timestamp;
                blacklist.N = signature.Nonce;
            }
            GenericCommand request = Client.NewCommand(CommandType.Blacklist, OpType.Block);
            request.BlacklistMessage = blacklist;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.BlacklistMessage.AllowedPids, response.BlacklistMessage.FailedPids);
        }

        internal async Task<LCIMPartiallySuccessResult> UnblockMembers(string convId, IEnumerable<string> clientIds) {
            BlacklistCommand blacklist = new BlacklistCommand {
                SrcCid = convId,
            };
            blacklist.ToPids.AddRange(clientIds);
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = Client.SignatureFactory.CreateBlacklistSignature(convId,
                    Client.Id,
                    clientIds,
                    LCIMSignatureAction.ConversationUnblockClients);
                blacklist.S = signature.Signature;
                blacklist.T = signature.Timestamp;
                blacklist.N = signature.Nonce;
            }
            GenericCommand request = Client.NewCommand(CommandType.Blacklist, OpType.Unblock);
            request.BlacklistMessage = blacklist;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.BlacklistMessage.AllowedPids, response.BlacklistMessage.FailedPids);
        }

        internal async Task UpdateMemberRole(string convId, string memberId, string role) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
                TargetClientId = memberId,
                Info = new ConvMemberInfo {
                    Pid = memberId,
                    Role = role
                }
            };
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.MemberInfoUpdate);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
        }

        internal async Task<List<LCIMConversationMemberInfo>> GetAllMemberInfo(string convId) {
            string path = "classes/_ConversationMemberInfo";
            string token = await Client.SessionController.GetToken();
            Dictionary<string, object> headers = new Dictionary<string, object> {
                { "X-LC-IM-Session-Token", token }
            };
            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { "client_id", Client.Id },
                { "cid", convId }
            };
            Dictionary<string, object> response = await LCApplication.HttpClient.Get<Dictionary<string, object>>(path,
                headers: headers, queryParams: queryParams);
            List<object> results = response["results"] as List<object>;
            List<LCIMConversationMemberInfo> memberList = new List<LCIMConversationMemberInfo>();
            foreach (Dictionary<string, object> item in results) {
                LCIMConversationMemberInfo member = new LCIMConversationMemberInfo {
                    ConversationId = item["cid"] as string,
                    MemberId = item["clientId"] as string,
                    Role = item["role"] as string
                };
                memberList.Add(member);
            }
            return memberList;
        }

        internal async Task<LCIMPageResult> QueryMutedMembers(string convId, int limit = 10, string next = null) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
                Limit = limit,
                Next = next
            };
            GenericCommand request = Client.NewCommand(CommandType.Conv, OpType.QueryShutup);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return new LCIMPageResult {
                Results = response.ConvMessage.M.ToList(),
                Next = response.ConvMessage.Next
            };
        }

        internal async Task<LCIMPageResult> QueryBlockedMembers(string convId, int limit = 10, string next = null) {
            BlacklistCommand black = new BlacklistCommand {
                SrcCid = convId,
                Limit = limit,
                Next = next
            };
            GenericCommand request = Client.NewCommand(CommandType.Blacklist, OpType.Query);
            request.BlacklistMessage = black;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return new LCIMPageResult {
                Results = response.BlacklistMessage.BlockedPids.ToList(),
                Next = response.BlacklistMessage.Next
            };
        }

        private LCIMPartiallySuccessResult NewPartiallySuccessResult(IEnumerable<string> succesfulIds, IEnumerable<ErrorCommand> errors) {
            LCIMPartiallySuccessResult result = new LCIMPartiallySuccessResult {
                SuccessfulClientIdList = succesfulIds.ToList()
            };
            if (errors != null) {
                result.FailureList = new List<LCIMOperationFailure>();
                foreach (ErrorCommand error in errors) {
                    result.FailureList.Add(new LCIMOperationFailure(error));
                }
            }
            return result;
        }

        internal override async Task OnNotification(GenericCommand notification) {
            ConvCommand conv = notification.ConvMessage;
            switch (notification.Op) {
                case OpType.Joined:
                    await OnConversationJoined(conv);
                    break;
                case OpType.MembersJoined:
                    await OnConversationMembersJoined(conv);
                    break;
                case OpType.Left:
                    await OnConversationLeft(conv);
                    break;
                case OpType.MembersLeft:
                    await OnConversationMemberLeft(conv);
                    break;
                case OpType.Updated:
                    await OnConversationPropertiesUpdated(conv);
                    break;
                case OpType.MemberInfoChanged:
                    await OnConversationMemberInfoChanged(conv);
                    break;
                default:
                    break;
            }
        }

        private async Task OnConversationJoined(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            conversation.MergeFrom(conv);
            Client.OnInvited?.Invoke(conversation, conv.InitBy);
        }

        private async Task OnConversationMembersJoined(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            conversation.MergeFrom(conv);
            Client.OnMembersJoined?.Invoke(conversation, conv.M.ToList(), conv.InitBy);
        }

        private async Task OnConversationLeft(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            Client.OnKicked?.Invoke(conversation, conv.InitBy);
        }

        private async Task OnConversationMemberLeft(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            List<string> leftIdList = conv.M.ToList();
            Client.OnMembersLeft?.Invoke(conversation, leftIdList, conv.InitBy);
        }

        private async Task OnConversationPropertiesUpdated(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            Dictionary<string, object> updatedAttr = JsonConvert.DeserializeObject<Dictionary<string, object>>(conv.AttrModified.Data,
                new LCJsonConverter());
            // 更新内存数据
            conversation.MergeInfo(updatedAttr);
            Client.OnConversationInfoUpdated?.Invoke(conversation, updatedAttr, conv.InitBy);
        }

        private async Task OnConversationMemberInfoChanged(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            ConvMemberInfo memberInfo = conv.Info;
            Client.OnMemberInfoUpdated?.Invoke(conversation, memberInfo.Pid, memberInfo.Role, conv.InitBy);
        }
    }
}
