using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LeanCloud.Realtime.Internal.Protocol;
using LeanCloud.Storage.Internal.Codec;
using LeanCloud.Common;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMConversationController : LCIMController {
        internal LCIMConversationController(LCIMClient client) : base(client) {

        }

        #region 内部接口

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
            if (temporary) {
                conv.TempConv = temporary;
                conv.TempConvTTL = temporaryTtl;
            }
            Dictionary<string, object> attrs = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) {
                attrs["name"] = name;
            }
            if (properties != null) {
                attrs = properties.Union(attrs.Where(kv => !properties.ContainsKey(kv.Key)))
                    .ToDictionary(k => k.Key, v => v.Value);
            }
            conv.Attr = new JsonObjectMessage {
                Data = JsonConvert.SerializeObject(LCEncoder.Encode(attrs))
            };
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
            // 将自己加入
            conversation.ids.Add(Client.Id);
            conversation.CreatedAt = DateTime.Parse(response.ConvMessage.Cdate);
            conversation.UpdatedAt = conversation.CreatedAt;
            return conversation;
        }


        internal async Task<int> GetMembersCount(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
            };
            GenericCommand command = NewCommand(CommandType.Conv, OpType.Count);
            command.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(command);
            return response.ConvMessage.Count;
        }


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
            GenericCommand response = await Connection.SendRequest(request);
            JsonObjectMessage attr = response.ConvMessage.AttrModified;
            // 更新自定义属性
            if (attr != null) {
                Dictionary<string, object> updatedAttr = JsonConvert.DeserializeObject<Dictionary<string, object>>(attr.Data);
                return updatedAttr;
            }
            return null;
        }


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
            GenericCommand response = await Connection.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> errors = response.ConvMessage.FailedPids.ToList();
            return NewPartiallySuccessResult(allowedIds, errors);
        }


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
            GenericCommand response = await Connection.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> errors = response.ConvMessage.FailedPids.ToList();
            return NewPartiallySuccessResult(allowedIds, errors);
        }

 
        internal async Task Mute(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Mute);
            request.ConvMessage = conv;
            await Connection.SendRequest(request);
        }

        internal async Task Unmute(string convId) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Unmute);
            request.ConvMessage = conv;
            await Connection.SendRequest(request);
        }

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
            GenericCommand response = await Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.ConvMessage.AllowedPids, response.ConvMessage.FailedPids);
        }


        internal async Task<LCIMPartiallySuccessResult> UnmuteMembers(string convId,
            IEnumerable<string> clientIds) {
            ConvCommand conv = new ConvCommand {
                Cid = convId
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = NewCommand(CommandType.Conv, OpType.RemoveShutup);
            request.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.ConvMessage.AllowedPids, response.ConvMessage.FailedPids);
        }


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
            GenericCommand response = await Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.BlacklistMessage.AllowedPids, response.BlacklistMessage.FailedPids);
        }


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
            GenericCommand response = await Connection.SendRequest(request);
            return NewPartiallySuccessResult(response.BlacklistMessage.AllowedPids, response.BlacklistMessage.FailedPids);
        }


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
            GenericCommand response = await Connection.SendRequest(request);
        }


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
            GenericCommand response = await Connection.SendRequest(request);
            return new LCIMPageResult {
                Results = new ReadOnlyCollection<string>(response.ConvMessage.M),
                Next = response.ConvMessage.Next
            };
        }

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
            GenericCommand response = await Connection.SendRequest(request);
            return new LCIMPageResult {
                Results = new ReadOnlyCollection<string>(response.BlacklistMessage.BlockedPids),
                Next = response.BlacklistMessage.Next
            };
        }


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
            List<object> convs = JsonConvert.DeserializeObject<List<object>>(results.Data,
                LCJsonConverter.Default);
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
            List<object> convs = JsonConvert.DeserializeObject<List<object>>(results.Data,
                LCJsonConverter.Default);
            List<LCIMTemporaryConversation> convList = convs.Select(item => {
                LCIMTemporaryConversation temporaryConversation = new LCIMTemporaryConversation(Client);
                temporaryConversation.MergeFrom(item as Dictionary<string, object>);
                return temporaryConversation;
            }).ToList();
            return convList;
        }


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


        internal async Task<ReadOnlyCollection<string>> GetOnlineMembers(string convId,
            int limit) {
            ConvCommand conv = new ConvCommand {
                Cid = convId,
                Limit = limit
            };
            GenericCommand request = NewCommand(CommandType.Conv, OpType.Members);
            request.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(request);
            ReadOnlyCollection<string> members = response.ConvMessage.M
                .ToList().AsReadOnly();
            return members;
        }


        internal async Task<bool> CheckSubscription(string convId) {
            ConvCommand conv = new ConvCommand();
            conv.Cids.Add(convId);
            GenericCommand request = NewCommand(CommandType.Conv, OpType.IsMember);
            request.ConvMessage = conv;
            GenericCommand response = await Connection.SendRequest(request);
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
                    LCIMOperationFailure failure = new LCIMOperationFailure {
                        Code = error.Code,
                        Reason = error.Reason,
                        IdList = error.Pids?.ToList()
                    };
                    result.FailureList.Add(failure);
                }
            }
            return result;
        }

        #endregion

        #region 消息处理

        internal override void HandleNotification(GenericCommand notification) {
            if (notification.Cmd == CommandType.Conv) {
                _ = OnConversation(notification);
            } else if (notification.Cmd == CommandType.Unread) {
                _ = OnUnread(notification);
            }
        }

        private async Task OnUnread(GenericCommand notification) {
            UnreadCommand unread = notification.UnreadMessage;

            IEnumerable<string> convIds = unread.Convs
                .Select(conv => conv.Cid);
            Dictionary<string, LCIMConversation> conversationDict = (await Client.GetConversationList(convIds))
                .ToDictionary(item => item.Id);
            ReadOnlyCollection<LCIMConversation> conversations = unread.Convs.Select(conv => {
                // 设置对话中的未读数据
                LCIMConversation conversation = conversationDict[conv.Cid];
                conversation.Unread = conv.Unread;
                if (conv.HasData || conv.HasBinaryMsg) {
                    // 如果有消息，则反序列化
                    LCIMMessage message = null;
                    if (conv.HasBinaryMsg) {
                        // 二进制消息
                        byte[] bytes = conv.BinaryMsg.ToByteArray();
                        message = LCIMBinaryMessage.Deserialize(bytes);
                    } else {
                        // 类型消息
                        message = LCIMTypedMessage.Deserialize(conv.Data);
                    }
                    // 填充消息数据
                    message.ConversationId = conv.Cid;
                    message.Id = conv.Mid;
                    message.FromClientId = conv.From;
                    message.SentTimestamp = conv.Timestamp;
                    message.Mentioned = conv.Mentioned;
                    conversation.LastMessage = message;
                }
                return conversation;
            }).ToList().AsReadOnly();
            Client.OnUnreadMessagesCountUpdated?.Invoke(conversations);
        }

        private async Task OnConversation(GenericCommand notification) {
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
                case OpType.Unblocked:
                    await OnUnblocked(convMessage);
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
                case OpType.Unshutuped:
                    await OnUnmuted(convMessage);
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


        private async Task OnJoined(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnInvited?.Invoke(conversation, convMessage.InitBy);
        }


        private async Task OnMembersJoined(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> joinedIds = new ReadOnlyCollection<string>(convMessage.M);
            conversation.ids.UnionWith(joinedIds);
            Client.OnMembersJoined?.Invoke(conversation, joinedIds, convMessage.InitBy);
        }


        private async Task OnLeft(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            // 从内存中清除对话
            Client.ConversationDict.Remove(conversation.Id);
            Client.OnKicked?.Invoke(conversation, convMessage.InitBy);
        }

        private async Task OnMemberLeft(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> leftIdList = new ReadOnlyCollection<string>(convMessage.M);
            conversation.ids.RemoveWhere(item => leftIdList.Contains(item));
            Client.OnMembersLeft?.Invoke(conversation, leftIdList, convMessage.InitBy);
        }


        private async Task OnMuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnMuted?.Invoke(conversation, convMessage.InitBy);
        }


        private async Task OnUnmuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnUnmuted?.Invoke(conversation, convMessage.InitBy);
        }


        private async Task OnMembersMuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> mutedMemberIds = new ReadOnlyCollection<string>(convMessage.M);
            conversation.mutedIds.UnionWith(mutedMemberIds);
            Client.OnMembersMuted?.Invoke(conversation, mutedMemberIds, convMessage.InitBy);
        }


        private async Task OnMembersUnmuted(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> unmutedMemberIds = new ReadOnlyCollection<string>(convMessage.M);
            conversation.mutedIds.RemoveWhere(id => unmutedMemberIds.Contains(id));
            Client.OnMembersUnmuted?.Invoke(conversation, unmutedMemberIds, convMessage.InitBy);
        }

        private async Task OnBlocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnBlocked?.Invoke(conversation, convMessage.InitBy);
        }


        private async Task OnUnblocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            Client.OnUnblocked?.Invoke(conversation, convMessage.InitBy);
        }


        private async Task OnMembersBlocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> blockedMemberIds = convMessage.M.ToList().AsReadOnly();
            Client.OnMembersBlocked?.Invoke(conversation, blockedMemberIds, convMessage.InitBy);
        }


        private async Task OnMembersUnblocked(ConvCommand convMessage) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(convMessage.Cid);
            ReadOnlyCollection<string> unblockedMemberIds = convMessage.M.ToList().AsReadOnly();
            Client.OnMembersUnblocked?.Invoke(conversation, unblockedMemberIds, convMessage.InitBy);
        }

        private async Task OnPropertiesUpdated(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            Dictionary<string, object> updatedAttr = JsonConvert.DeserializeObject<Dictionary<string, object>>(conv.AttrModified.Data,
                LCJsonConverter.Default);
            // 更新内存数据
            conversation.MergeInfo(updatedAttr);
            Client.OnConversationInfoUpdated?.Invoke(conversation,
                new ReadOnlyDictionary<string, object>(updatedAttr),
                conv.InitBy);
        }


        private async Task OnMemberInfoChanged(ConvCommand conv) {
            LCIMConversation conversation = await Client.GetOrQueryConversation(conv.Cid);
            ConvMemberInfo memberInfo = conv.Info;
            Client.OnMemberInfoUpdated?.Invoke(conversation, memberInfo.Pid, memberInfo.Role, conv.InitBy);
        }

        #endregion
    }
}
