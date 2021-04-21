using System;
using System.Threading.Tasks;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMServiceConversation is a local representation of service conversation
    /// in LeanCloud.
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

        public override Task FetchReciptTimestamps() {
            return Task.CompletedTask;
        }
    }
}
