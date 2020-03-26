using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Google.Protobuf;
using LeanCloud.Storage.Internal;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime.Internal.Controller {
    internal class LCIMMessageController : LCIMController {
        internal LCIMMessageController(LCIMClient client) : base(client) {

        }

        internal async Task<LCIMMessage> Send(string convId,
            LCIMMessage message) {
            DirectCommand direct = new DirectCommand {
                FromPeerId = Client.Id,
                Cid = convId,
            };
            if (message is LCIMTypedMessage typedMessage) {
                direct.Msg = JsonConvert.SerializeObject(typedMessage.Encode());
            } else if (message is LCIMBinaryMessage binaryMessage) {
                direct.BinaryMsg = ByteString.CopyFrom(binaryMessage.Data);
            } else {
                throw new ArgumentException("Message MUST BE LCIMTypedMessage or LCIMBinaryMessage.");
            }
            GenericCommand command = Client.NewDirectCommand();
            command.DirectMessage = direct;
            GenericCommand response = await Client.Connection.SendRequest(command);
            // 消息发送应答
            AckCommand ack = response.AckMessage;
            message.Id = ack.Uid;
            message.DeliveredTimestamp = ack.T;
            return message;
        }

        internal async Task RecallMessage(string convId,
            LCIMMessage message) {
            PatchCommand patch = new PatchCommand();
            PatchItem item = new PatchItem {
                Cid = convId,
                Mid = message.Id,
                Recall = true
            };
            patch.Patches.Add(item);
            GenericCommand request = Client.NewCommand(CommandType.Patch, OpType.Modify);
            request.PatchMessage = patch;
            await Client.Connection.SendRequest(request);
        }

        internal async Task UpdateMessage(string convId,
            LCIMMessage oldMessage,
            LCIMMessage newMessage) {
            PatchCommand patch = new PatchCommand();
            PatchItem item = new PatchItem {
                Cid = convId,
                Mid = oldMessage.Id,
                Timestamp = oldMessage.DeliveredTimestamp,
                Recall = false,
            };
            if (newMessage is LCIMTypedMessage typedMessage) {
                item.Data = JsonConvert.SerializeObject(typedMessage.Encode());
            } else if (newMessage is LCIMBinaryMessage binaryMessage) {
                item.BinaryMsg = ByteString.CopyFrom(binaryMessage.Data);
            }
            if (newMessage.MentionList != null) {
                item.MentionPids.AddRange(newMessage.MentionList);
            }
            if (newMessage.MentionAll) {
                item.MentionAll = newMessage.MentionAll;
            }
            patch.Patches.Add(item);
            GenericCommand request = Client.NewCommand(CommandType.Patch, OpType.Modify);
            request.PatchMessage = patch;
            GenericCommand response = await Client.Connection.SendRequest(request);
        }

        internal async Task<List<LCIMMessage>> QueryMessages(string convId,
            LCIMMessageQueryEndpoint start = null,
            LCIMMessageQueryEndpoint end = null,
            LCIMMessageQueryDirection direction = LCIMMessageQueryDirection.NewToOld,
            int limit = 20,
            int messageType = 0) {
            LogsCommand logs = new LogsCommand {
                Cid = convId
            };
            if (start != null) {
                logs.T = start.SentTimestamp;
                logs.Mid = start.MessageId;
                logs.TIncluded = start.IsClosed;
            }
            if (end != null) {
                logs.Tt = end.SentTimestamp;
                logs.Tmid = end.MessageId;
                logs.TtIncluded = end.IsClosed;
            }
            logs.Direction = direction == LCIMMessageQueryDirection.NewToOld ?
                LogsCommand.Types.QueryDirection.Old : LogsCommand.Types.QueryDirection.New;
            logs.Limit = limit;
            if (messageType != 0) {
                logs.Lctype = messageType;
            }
            GenericCommand request = Client.NewCommand(CommandType.Logs, OpType.Open);
            request.LogsMessage = logs;
            GenericCommand response = await Client.Connection.SendRequest(request);
            // TODO 反序列化聊天记录

            return null;
        }

        internal override async Task OnNotification(GenericCommand notification) {
            DirectCommand direct = notification.DirectMessage;
            LCIMMessage message = null;
            if (direct.HasBinaryMsg) {
                // 二进制消息
                byte[] bytes = direct.BinaryMsg.ToByteArray();
                message = new LCIMBinaryMessage(bytes);
            } else {
                // 文本消息
                string messageData = direct.Msg;
                Dictionary<string, object> msgData = JsonConvert.DeserializeObject<Dictionary<string, object>>(messageData,
                    new LCJsonConverter());
                int msgType = (int)msgData["_lctype"];
                switch (msgType) {
                    case -1:
                        message = new LCIMTextMessage();
                        break;
                    case -2:
                        message = new LCIMImageMessage();
                        break;
                    case -3:
                        message = new LCIMAudioMessage();
                        break;
                    case -4:
                        message = new LCIMVideoMessage();
                        break;
                    case -5:
                        message = new LCIMLocationMessage();
                        break;
                    case -6:
                        message = new LCIMFileMessage();
                        break;
                    default:
                        break;
                }
                message.Decode(direct);
            }
            // 获取对话
            LCIMConversation conversation = await Client.GetOrQueryConversation(direct.Cid);
            Client.OnMessage?.Invoke(conversation, message);
        }
    }
}
