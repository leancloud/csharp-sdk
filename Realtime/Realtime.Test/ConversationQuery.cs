using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Realtime;

namespace Realtime.Test {
    public class ConversationQuery {
        private string clientId = "hello123";
        private LCIMClient client;

        [SetUp]
        public async Task SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
            client = new LCIMClient(clientId);
            await client.Open();
        }

        [TearDown]
        public async Task TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
            await client.Close();
        }

        [Test]
        public async Task QueryMyConversation() {
            LCIMConversationQuery query = new LCIMConversationQuery(client);
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            Assert.Greater(conversations.Count, 0);
            foreach (LCIMConversation conversation in conversations) {
                Assert.True(conversation.MemberIds.Contains(clientId));
            }
        }

        [Test]
        public async Task QueryMemberConversation() {
            string memberId = "cc1";
            LCIMConversationQuery query = new LCIMConversationQuery(client);
            query.WhereEqualTo("m", memberId);
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            Assert.Greater(conversations.Count, 0);
            foreach (LCIMConversation conversation in conversations) {
                Assert.True(conversation.MemberIds.Contains(memberId));
            }
        }
    }
}
