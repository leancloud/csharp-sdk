using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using LeanCloud.Realtime.Protocol;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Realtime {
    public class LCIMConversation {
        public string Id {
            get; set;
        }

        public string Name {
            get {
                return this["name"] as string;
            } set {
                this["name"] = value;
            }
        }

        public string CreatorId {
            get; set;
        }

        public List<string> MemberIdList {
            get; set;
        }

        public DateTime CreatedAt {
            get; set;
        }

        public DateTime UpdatedAt {
            get; set;
        }

        public DateTime LastMessageAt {
            get; internal set;
        }

        public object this[string key] {
            get {
                return customProperties[key];
            }
            set {
                customProperties[key] = value;
            }
        }

        public bool IsMute {
            get; private set;
        }

        public virtual bool IsSystem => false;

        public virtual bool IsTransient => false;

        private readonly LCIMClient client;

        private Dictionary<string, object> customProperties;

        internal LCIMConversation(LCIMClient client) {
            this.client = client;
            customProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取对话人数，或暂态对话的在线人数
        /// </summary>
        /// <returns></returns>
        public async Task<int> Count() {
            ConvCommand conv = new ConvCommand {
                Cid = Id,
            };
            GenericCommand command = client.NewCommand(CommandType.Conv, OpType.Count);
            command.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(command);
            return response.ConvMessage.Count;
        }

        public async Task<LCIMConversation> Save() {
            ConvCommand conv = new ConvCommand {
                Cid = Id,
            };
            // 注意序列化是否与存储一致
            string json = JsonConvert.SerializeObject(LCEncoder.Encode(customProperties));
            conv.Attr = new JsonObjectMessage {
                Data = json
            };
            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.Update);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);
            JsonObjectMessage attr = response.ConvMessage.AttrModified;
            // 更新自定义属性
            if (attr != null) {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(attr.Data);
                Dictionary<string, object> objectData = LCDecoder.Decode(data) as Dictionary<string, object>;
                foreach (KeyValuePair<string, object> kv in objectData) {
                    customProperties[kv.Key] = kv.Value;
                }
            }
            return this;
        }

        /// <summary>
        /// 添加用户到对话
        /// </summary>
        /// <param name="clientIds">用户 Id</param>
        /// <returns></returns>
        public async Task<LCIMConversation> Add(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            ConvCommand conv = new ConvCommand {
                Cid = Id,
            };
            conv.M.AddRange(clientIds);
            // TODO 签名参数

            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.Add);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> failedIds = response.ConvMessage.FailedPids.ToList();
            // TODO 转化为返回

            return this;
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="removeIds">用户 Id</param>
        /// <returns></returns>
        public async Task<LCIMConversation> Remove(IEnumerable<string> removeIds) {
            if (removeIds == null || removeIds.Count() == 0) {
                throw new ArgumentNullException(nameof(removeIds));
            }
            ConvCommand conv = new ConvCommand {
                Cid = Id,
            };
            conv.M.AddRange(removeIds);
            // TODO 签名参数

            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.Remove);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);
            List<string> allowedIds = response.ConvMessage.AllowedPids.ToList();
            List<ErrorCommand> failedIds = response.ConvMessage.FailedPids.ToList();
            // TODO 转化为返回

            return this;
        }

        /// <summary>
        /// 加入对话
        /// </summary>
        /// <returns></returns>
        public async Task<LCIMConversation> Join() {
            return await Add(new string[] { client.ClientId });
        }

        /// <summary>
        /// 离开对话
        /// </summary>
        /// <returns></returns>
        public async Task<LCIMConversation> Quit() {
            return await Remove(new string[] { client.ClientId });
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<LCIMMessage> Send(LCIMMessage message) {
            DirectCommand direct = new DirectCommand {
                FromPeerId = client.ClientId,
                Cid = Id,
                Msg = message.Serialize(),
            };
            GenericCommand command = client.NewDirectCommand();
            command.DirectMessage = direct;
            GenericCommand response = await client.client.SendRequest(command);
            // 消息发送应答
            AckCommand ack = response.AckMessage;
            message.Id = ack.Uid;
            message.DeliveredTimestamp = ack.T;
            return message;
        }

        public async Task<LCIMRecalledMessage> Recall(LCIMMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            PatchCommand patch = new PatchCommand();
            PatchItem item = new PatchItem {
                Cid = Id,
                Mid = message.Id,
                Recall = true
            };
            patch.Patches.Add(item);
            GenericCommand request = client.NewCommand(CommandType.Patch, OpType.Modify);
            request.PatchMessage = patch;
            GenericCommand response = await client.client.SendRequest(request);
            return null;
        }

        /// <summary>
        /// 静音
        /// </summary>
        /// <returns></returns>
        public async Task<LCIMConversation> Mute() {
            ConvCommand conv = new ConvCommand {
                Cid = Id
            };
            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.Mute);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);
            IsMute = true;
            return this;
        }

        /// <summary>
        /// 取消静音
        /// </summary>
        /// <returns></returns>
        public async Task<LCIMConversation> Unmute() {
            ConvCommand conv = new ConvCommand {
                Cid = Id
            };
            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.Unmute);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);
            IsMute = false;
            return this;
        }

        /// <summary>
        /// 禁言
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public async Task MuteMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            ConvCommand conv = new ConvCommand {
                Cid = Id
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.AddShutup);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);

        }

        /// <summary>
        /// 取消禁言
        /// </summary>
        /// <param name="clientIdList"></param>
        /// <returns></returns>
        public async Task UnmuteMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            ConvCommand conv = new ConvCommand {
                Cid = Id
            };
            conv.M.AddRange(clientIds);
            GenericCommand request = client.NewCommand(CommandType.Conv, OpType.Remove);
            request.ConvMessage = conv;
            GenericCommand response = await client.client.SendRequest(request);
        }

        /// <summary>
        /// 将用户加入黑名单
        /// </summary>
        /// <param name="clientIds"></param>
        /// <returns></returns>
        public async Task BlockMembers(IEnumerable<string> clientIds) {
            if (clientIds == null || clientIds.Count() == 0) {
                throw new ArgumentNullException(nameof(clientIds));
            }
            BlacklistCommand blacklist = new BlacklistCommand {
                SrcCid = Id,
                
            };
            GenericCommand request = client.NewCommand(CommandType.Blacklist, OpType.Block);
            request.BlacklistMessage = blacklist;
            await client.client.SendRequest(request);
        }

        public async Task UnblockMembers(IEnumerable<string> clientIds) {

        }

        public async Task<LCIMMessage> Update(LCIMMessage oldMessage, LCIMMessage newMessage) {
            return null;
        }

        public async Task<LCIMConversation> UpdateMemberRole(string memberId, string role) {
            return this;
        }

        public async Task<LCIMConversationMemberInfo> GetMemberInfo(string memberId) {
            if (string.IsNullOrEmpty(memberId)) {
                throw new ArgumentNullException(nameof(memberId));
            }

            return null;
        }

        public async Task<List<LCIMConversationMemberInfo>> GetAllMemberInfo() {
            return null;
        }

        internal void MergeFrom(ConvCommand conv) {
            if (conv.HasCid) {
                Id = conv.Cid;
            }
            if (conv.HasInitBy) {
                CreatorId = conv.InitBy;
            }
            if (conv.HasCdate) {
                CreatedAt = DateTime.Parse(conv.Cdate);
            }
            if (conv.HasUdate) {
                UpdatedAt = DateTime.Parse(conv.Udate);
            }
            if (conv.M.Count > 0) {
                MemberIdList = conv.M.ToList();
            }
        }
    }
}
