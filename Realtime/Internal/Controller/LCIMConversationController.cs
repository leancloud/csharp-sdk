using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Common;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMConversationController : LCIMController {
        internal LCIMConversationController(LCIMClient client) : base(client) {

        }

        #region 内部接口

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
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Start);
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
                LCIMSignature signature = await Client.SignatureFactory.CreateStartConversationSignature(Client.Id, members);
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
            conversation.Id = convId;
            conversation.Unique = unique;
            conversation.UniqueId = response.ConvMessage.UniqueId;
            conversation.Name = name;
            conversation.CreatorId = Client.Id;
            conversation.ids = members != null ?
                new HashSet<string>(members) : new HashSet<string>();
            conversation.CreatedAt = DateTime.Parse(response.ConvMessage.Cdate);
            conversation.UpdatedAt = conversation.CreatedAt;
            return conversation;
        }

        /// <summary>
        /// 查询成员数量
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        internal async Task<int> GetMembersCount(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            GenericCommand command = NewCommand(CommandType.Conv, OpType.Count);
            command.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(command);
            return response.ConvMessage.Count;
        }

        /// <summary>
        /// 更新对话属性
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        internal async Task<Dictionary<string, object>> UpdateInfo(string convId,
            Dictionary<string, object> attributes) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            conv.Attr = new JsonObjectMessage {
                Data = JsonConvert.SerializeObject(attributes)
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Update);
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

        /// <summary>
        /// 增加成员
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        internal async Task<LCIMPartiallySuccessResult> AddMembers(string convId,
            IEnumerable<string> clientIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            conv.M.AddRange(clientIds);
            // 签名参数
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = await Client.SignatureFactory.CreateConversationSignature(convId,
                    Client.Id,
                    clientIds,
                    LCIMSignatureAction.Invite);
                conv.S = signature.Signature;
                conv.T = signature.Timestamp;
                conv.N = signature.Nonce;
            }
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Add);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> errors = response.ConvMessage.FailedPids.ToList();
            return NewPartiallySuccessResult(allowedIds, errors);
        }

        /// <summary>
        /// 移除成员
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="removeIds"></param>
        /// <returns></returns>
        internal async Task<LCIMPartiallySuccessResult> RemoveMembers(string convId,
            IEnumerable<string> removeIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            conv.M.AddRange(removeIds);
            // 签名参数
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = await Client.SignatureFactory.CreateConversationSignature(convId,
                    Client.Id,
                    removeIds,
                    LCIMSignatureAction.Kick);
                conv.S = signature.Signature;
                conv.T = signature.Timestamp;
                conv.N = signature.Nonce;
            }
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Remove);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> errors = response.ConvMessage.FailedPids.ToList();
            return NewPartiallySuccessResult(allowedIds, errors);
        }

        /// <summary>
        /// 静音
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        internal async Task Mute(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Mute);
            request.ConvMessage = conv;
            await Client.Connection.SendRequest(request);
        }

        /// <summary>
        /// 解除静音
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        internal async Task Unmute(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Unmute);
            request.ConvMessage = conv;
            await Client.Connection.SendRequest(request);
        }

        /// <summary>
        /// 禁言用户
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        internal async Task<LCIMPartiallySuccessResult> MuteMembers(string convId,
            IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = NewCommand(CommandType.Conv, OpType.AddShutup);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.ConvMessage.AllowedPids, response.ConvMessage.FailedPids);
        }

        /// <summary>
        /// 解除用户禁言
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        internal async Task<LCIMPartiallySuccessResult> UnmuteMembers(string convId,
            IEnumerable<string> clientIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Remove);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.ConvMessage.AllowedPids, response.ConvMessage.FailedPids);
        }

        /// <summary>
        /// 拉黑成员
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        internal async Task<LCIMPartiallySuccessResult> BlockMembers(string convId,
            IEnumerable<string> clientIds) {
            BlacklistCommand blacklist = new BlacklistCommand {
                SrcCid = convId,
            };
            blacklist.ToPids.AddRange(clientIds);
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = await Client.SignatureFactory.CreateBlacklistSignature(convId,
                    Client.Id,
                    clientIds,
                    LCIMSignatureAction.ConversationBlockClients);
                blacklist.S = signature.Signature;
                blacklist.T = signature.Timestamp;
                blacklist.N = signature.Nonce;
            }
            GenericCommand request = NewCommand(CommandType.Blacklist, OpType.Block);
            request.BlacklistMessage = blacklist;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.BlacklistMessage.AllowedPids, response.BlacklistMessage.FailedPids);
        }

        /// <summary>
        /// 移除成员黑名单
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        internal async Task<LCIMPartiallySuccessResult> UnblockMembers(string convId,
            IEnumerable<string> clientIds) {
            BlacklistCommand blacklist = new BlacklistCommand {
                SrcCid = convId,
            };
            blacklist.ToPids.AddRange(clientIds);
            if (Client.SignatureFactory != null) {
                LCIMSignature signature = await Client.SignatureFactory.CreateBlacklistSignature(convId,
                    Client.Id,
                    clientIds,
                    LCIMSignatureAction.ConversationUnblockClients);
                blacklist.S = signature.Signature;
                blacklist.T = signature.Timestamp;
                blacklist.N = signature.Nonce;
            }
            GenericCommand request = NewCommand(CommandType.Blacklist, OpType.Unblock);
            request.BlacklistMessage = blacklist;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.BlacklistMessage.AllowedPids, response.BlacklistMessage.FailedPids);
        }

        /// <summary>
        /// 修改成员角色
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="memberId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        internal async Task UpdateMemberRole(string convId,
            string memberId,
            string role) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
                TargetClientId = memberId,
                Info = new ConvMemberInfo {
                    Pid = memberId,
                    Role = role
                }
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.MemberInfoUpdate);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
        }

        /// <summary>
        /// 获取所有成员角色
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        internal async Task<ReadOnlyCollection<LCIMConversationMemberInfo>> GetAllMemberInfo(string convId) {
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
            return results.Select(item => {
                Dictionary<string, object> memberInfo = item as Dictionary<string, object>;
                return new LCIMConversationMemberInfo {
                    ConversationId = memberInfo["cid"] as string,
                    MemberId = memberInfo["clientId"] as string,
                    Role = memberInfo["role"] as string
                };
            }).ToList().AsReadOnly();
        }

        /// <summary>
        /// 查询禁言成员
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="limit"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        internal async Task<LCIMPageResult> QueryMutedMembers(string convId,
            int limit = 10,
            string next = null) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
                Limit = limit
            };
            if (next != null) {
                conv.Next = next;
            }
            GenericCommand request = NewCommand(CommandType.Conv, OpType.QueryShutup);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return new LCIMPageResult {
                Results = new ReadOnlyCollection<string>(response.ConvMessage.M),
                Next = response.ConvMessage.Next
            };
        }

        /// <summary>
        /// 查询黑名单用户
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="limit"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        internal async Task<LCIMPageResult> QueryBlockedMembers(string convId,
            int limit = 10,
            string next = null) {
            BlacklistCommand black = new BlacklistCommand {
                SrcCid = convId,
                Limit = limit
            };
            if (next != null) {
                black.Next = next;
            }
            GenericCommand request = NewCommand(CommandType.Blacklist, OpType.Query);
            request.BlacklistMessage = black;
            GenericCommand response = await Client.Connection.SendRequest(request);
            return new LCIMPageResult {
                Results = new ReadOnlyCollection<string>(response.BlacklistMessage.BlockedPids),
                Next = response.BlacklistMessage.Next
            };
        }

        /// <summary>
        /// 查找
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        internal async Task<ReadOnlyCollection<LCIMConversation>> Find(LCIMConversationQuery query) {
            GenericCommand command = new GenericCommand {
                Cmd = CommandType.Conv,
                Op = OpType.Query,
                AppId = LCApplication.AppId,
                PeerId = Client.Id,
            };
            ConvCommand convMessage = new ConvCommand();
            string where = query.Condition.BuildWhere();
            if (!string.IsNullOrEmpty(where)) {
                try {
                    convMessage.Where = new JsonObjectMessage {
                        Data = where
                    };
                } catch (Exception e) {
                    LCLogger.Error(e);
                }
            }
            command.ConvMessage = convMessage;
            GenericCommand response = await Connection.SendRequest(command);
            JsonObjectMessage results = response.ConvMessage.Results;
            List<object> convs = JsonConvert.DeserializeObject<List<object>>(results.Data, new LCJsonConverter());
            return convs.Select(item => {
                Dictionary<string, object> conv = item as Dictionary<string, object>;
                string convId = conv["objectId"] as string;
                if (!Client.ConversationDict.TryGetValue(convId, out LCIMConversation conversation)) {
                    // 解析是哪种类型的对话
                    if (conv.TryGetValue("tr", out object transient) && (bool)transient == true) {
                        conversation = new LCIMChatRoom(Client);
                    } else if (conv.ContainsKey("tempConv") && conv.ContainsKey("tempConvTTL")) {
                        conversation = new LCIMTemporaryConversation(Client);
                    } else if (conv.TryGetValue("sys", out object sys) && (bool)sys == true) {
                        conversation = new LCIMServiceConversation(Client);
                    } else {
                        conversation = new LCIMConversation(Client);
                    }
                    Client.ConversationDict[convId] = conversation;
                }
                conversation.MergeFrom(conv);
                return conversation;
            }).ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取临时对话
        /// </summary>
        /// <param name="convIds"></param>
        /// <returns></returns>
        internal async Task<List<LCIMTemporaryConversation>> GetTemporaryConversations(IEnumerable<string> convIds) {
            if (convIds == null || convIds.Count() == 0) {
                return null;
            }
            ConvCommand convMessage = new ConvCommand();
            convMessage.TempConvIds.AddRange(convIds);
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Query);
            request.ConvMessage = convMessage;
            GenericCommand response = await Connection.SendRequest(request);
            JsonObjectMessage results = response.ConvMessage.Results;
            List<object> convs = JsonConvert.DeserializeObject<List<object>>(results.Data, new LCJsonConverter());
            List<LCIMTemporaryConversation> convList = convs.Select(item => {
                LCIMTemporaryConversation temporaryConversation = new LCIMTemporaryConversation(Client);
                temporaryConversation.MergeFrom(item as Dictionary<string, object>);
                return temporaryConversation;
            }).ToList();
            return convList;
        }

        /// <summary>
        /// 拉取对话接收/已读情况
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        internal async Task FetchReciptTimestamp(string convId) {
            ConvCommand convCommand = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.MaxRead);
            request.ConvMessage = convCommand;
            GenericCommand response = await Connection.SendRequest(request);
            convCommand = response.ConvMessage;
            LCIMConversation conversation = await Client.GetOrQueryConversation(convCommand.Cid);
            conversation.LastDeliveredTimestamp = convCommand.MaxAckTimestamp;
            conversation.LastReadTimestamp = convCommand.MaxReadTimestamp;
        }

        /// <summary>
        /// 获取在线成员
        /// </summary>
        /// <param name="convId"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        internal async Task<ReadOnlyCollection<string>> GetOnlineMembers(string convId,
            int limit) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
                Limit = limit
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Members);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            ReadOnlyCollection<string> members = response.ConvMessage.M
                .ToList().AsReadOnly();
            return members;
        }

        /// <summary>
        /// 查询是否订阅
        /// </summary>
        /// <param name="convId"></param>
        /// <returns></returns>
        internal async Task<bool> CheckSubscription(string convId) {
            ConvCommand conv = new ConvCommand();
            conv.Cids.Add(convId);
            GenericCommand request = NewCommand(CommandType.Conv, OpType.IsMember);
            request.ConvMessage = conv;
            GenericCommand response = await Client.Connection.SendRequest(request);
            JsonObjectMessage jsonObj = response.ConvMessage.Results;
            Dictionary<string, object> result = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObj.Data);
            if (result.TryGetValue(convId, out object obj)) {
                return (bool)obj;
            }
            return false;
        }

        private LCIMPartiallySuccessResult NewPartiallySuccessResult(IEnumerable<string> succesfulIds,
            IEnumerable<ErrorCommand> errors) {
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

        #endregion

        #region 消息处理

        internal override async Task OnNotification(GenericCommand notification) {
            ConvCommand convMessage = notification.ConvMessage;
            switch (notification.Op) {
                case OpType.Joined:
                    await OnJoined(convMessage);
                    break;
                case OpType.MembersJoined:
                    await OnMembersJoined(convMessage);
                    break;
                case OpType.Left:
                    await OnLeft(convMessage);
                    break;
                case OpType.MembersLeft:
                    await OnMemberLeft(convMessage);
                    break;
                case OpType.Blocked:
                    await OnBlocked(convMessage);
                    break;
                case OpType.MembersBlocked:
                    await OnMembersBlocked(convMessage);
                    break;
                case OpType.MembersUnblocked:
                    await OnMembersUnblocked(convMessage);
                    break;
                case OpType.Shutuped:
                    await OnMuted(convMessage);
                    break;
                case OpType.MembersShutuped:
                    await OnMembersMuted(convMessage);
                    break;
                case OpType.MembersUnshutuped:
                    await OnMembersUnmuted(convMessage);
                    break;
                case OpType.Updated:
                    await OnPropertiesUpdated(convMessage);
                    break;
                case OpType.MemberInfoChanged:
                    await OnMemberInfoChanged(convMessage);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 当前用户加入会话
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnJoined(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnInvited?.Invoke(conversation, convMessage.InitBy);
        }

        /// <summary>
        /// 有用户加入会话
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMembersJoined(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> joinedIds = new ReadOnlyCollection<string>(convMessage.M);
            conversation.ids.UnionWith(joinedIds);
            Client.OnMembersJoined?.Invoke(conversation, joinedIds, convMessage.InitBy);
        }

        /// <summary>
        /// 当前用户离开会话
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnLeft(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            // TODO 从内存中清除对话

            Client.OnKicked?.Invoke(conversation, convMessage.InitBy);
        }

        /// <summary>
        /// 有成员离开会话
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMemberLeft(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> leftIdList = new ReadOnlyCollection<string>(convMessage.M);
            conversation.ids.RemoveWhere(item => leftIdList.Contains(item));
            Client.OnMembersLeft?.Invoke(conversation, leftIdList, convMessage.InitBy);
        }

        /// <summary>
        /// 当前用户被禁言
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnMuted?.Invoke(conversation, convMessage.InitBy);
        }

        /// <summary>
        /// 有成员被禁言
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMembersMuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> mutedMemberIds = new ReadOnlyCollection<string>(convMessage.M);
            conversation.mutedIds.UnionWith(mutedMemberIds);
            Client.OnMembersMuted?.Invoke(conversation, mutedMemberIds, convMessage.InitBy);
        }

        /// <summary>
        /// 有成员被解除禁言
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMembersUnmuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> unmutedMemberIds = new ReadOnlyCollection<string>(convMessage.M);
            conversation.mutedIds.RemoveWhere(id => unmutedMemberIds.Contains(id));
            Client.OnMembersUnmuted?.Invoke(conversation, unmutedMemberIds, convMessage.InitBy);
        }

        /// <summary>
        /// 当前用户被拉黑
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnBlocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnBlocked?.Invoke(conversation, convMessage.InitBy);
        }

        /// <summary>
        /// 有用户被拉黑
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMembersBlocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> blockedMemberIds = convMessage.M.ToList().AsReadOnly();
            Client.OnMembersBlocked?.Invoke(conversation, blockedMemberIds, convMessage.InitBy);
        }

        /// <summary>
        /// 有用户被移除黑名单
        /// </summary>
        /// <param name="convMessage"></param>
        /// <returns></returns>
        private async Task OnMembersUnblocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> unblockedMemberIds = convMessage.M.ToList().AsReadOnly();
            Client.OnMembersUnblocked?.Invoke(conversation, unblockedMemberIds, convMessage.InitBy);
        }

        /// <summary>
        /// 对话属性被修改
        /// </summary>
        /// <param name="conv"></param>
        /// <returns></returns>
        private async Task OnPropertiesUpdated(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            Dictionary<string, object> updatedAttr = JsonConvert.DeserializeObject<Dictionary<string, object>>(conv.AttrModified.Data,
                new LCJsonConverter());
            // 更新内存数据
            conversation.MergeInfo(updatedAttr);
            Client.OnConversationInfoUpdated?.Invoke(conversation,
                new ReadOnlyDictionary<string, object>(updatedAttr),
                conv.InitBy);
        }

        /// <summary>
        /// 用户角色被修改
        /// </summary>
        /// <param name="conv"></param>
        /// <returns></returns>
        private async Task OnMemberInfoChanged(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            ConvMemberInfo memberInfo = conv.Info;
            Client.OnMemberInfoUpdated?.Invoke(conversation, memberInfo.Pid, memberInfo.Role, conv.InitBy);
        }

        #endregion
    }
}
