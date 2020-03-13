using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Realtime.Protocol;

namespace LeanCloud.Realtime {
    public class LCIMConversation {
        public string Id {
            get; set;
        }

        public string Name {
            get; set;
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

        public bool IsMute => false;

        public virtual bool IsSystem => false;

        public virtual bool IsTransient => false;

        private readonly LCIMClient client;

        internal LCIMConversation(LCIMClient client) {
            this.client = client;
        }

        public void Set(string key, object value) {
            // 自定义属性

        }

        public async Task<int> Count() {
            return 0;
        }

        public async Task<LCIMConversation> Save() {
            return this;
        }

        public async Task Add(List<string> clientIdList) {

        }

        public async Task Remove(List<string> removeIdList) {

        }

        public async Task<LCIMConversation> Join() {
            return this;
        }

        public async Task<LCIMConversation> Quit() {
            return this;
        }

        public async Task<LCIMMessage> Send(LCIMMessage message) {
            return null;
        }

        public async Task<LCIMRecalledMessage> Recall(LCIMMessage message) {
            return null;
        }

        public async Task<LCIMConversation> Mute() {
            return this;
        }

        public async Task<LCIMConversation> Unmute() {
            return this;
        }

        public async Task MuteMemberList(List<string> clientIdList) {

        }

        public async Task UnmuteMemberList(List<string> clientIdList) {

        }

        public async Task BlockMemberList(List<string> clientIdList) {

        }

        public async Task UnblockMemberList(List<string> clientIdList) {

        }

        public async Task<LCIMMessage> Update(LCIMMessage oldMessage, LCIMMessage newMessage) {
            return null;
        }

        public async Task<LCIMConversation> UpdateMemberRole(string memberId, string role) {
            return this;
        }

        public async Task<LCIMConversationMemberInfo> GetMemberInfo(string memberId) {
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
