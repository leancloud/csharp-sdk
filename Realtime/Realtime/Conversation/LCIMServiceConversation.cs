using System;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    /// <summary>
    /// 系统对话
    /// </summary>
    public class LCIMServiceConversation : LCIMConversation {
        public LCIMServiceConversation(LCIMClient client) : base(client) {
        }

        public async Task Subscribe() {
            await Join();
        }

        public async Task Unsubscribe() {
            await Quit();
        }

        public async Task<bool> CheckSubscription() {
            return await Client.ConversationController.CheckSubscription(Id);
        }
    }
}
