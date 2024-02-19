using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud.Realtime;

namespace Realtime.Test {
    public class ConversationQuery : BaseTest {
        private readonly string clientId = "m1";
        private LCIMClient client;

        [SetUp]
        public override async Task SetUp() {
            await base.SetUp();
            client = new LCIMClient(clientId);
            await client.Open();
        }

        [TearDown]
        public override async Task TearDown() {
            await client.Close();
            await base.TearDown();
        }

        [Test]
        public async Task QueryMyConversation() {
            LCIMConversationQuery query = new LCIMConversationQuery(client);
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            Assert.Greater(conversations.Count, 0);
            foreach (LCIMConversation conversation in conversations) {
                Assert.True(conversation.MemberIds.Contains(clientId));
                Assert.Greater(conversation.LastMessageAt, System.DateTime.UnixEpoch);
            }
        }

        [Test]
        public async Task QueryMemberConversation() {
            string memberId = "m1";
            LCIMConversationQuery query = new LCIMConversationQuery(client);
            query.WhereEqualTo("m", memberId);
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            Assert.Greater(conversations.Count, 0);
            foreach (LCIMConversation conversation in conversations) {
                Assert.True(conversation.MemberIds.Contains(memberId));
            }
        }

        [Test]
        [Timeout(20000)]
        public async Task QueryCompact() {
            string memberId = "m1";
            LCIMConversationQuery query = new LCIMConversationQuery(client)
                .WhereEqualTo("m", memberId)
                .Limit(10);
            query.Compact = true;
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            foreach (LCIMConversation conversation in conversations) {
                Assert.True(conversation.MemberIds.Count == 0);
                await conversation.Fetch();
                Assert.True(conversation.MemberIds.Count > 0);
            }
        }

        [Test]
        public async Task QueryWithLastMessage() {
            string memberId = "m1";
            LCIMConversationQuery query = new LCIMConversationQuery(client)
                .WhereEqualTo("m", memberId);
            query.WithLastMessageRefreshed = true;
            ReadOnlyCollection<LCIMConversation> conversations = await query.Find();
            foreach (LCIMConversation conversation in conversations) {
                Assert.True(!string.IsNullOrEmpty(conversation.LastMessage.Id));
                if (conversation.LastMessage is LCIMBinaryMessage binaryMessage) {
                    TestContext.WriteLine(System.Text.Encoding.UTF8.GetString(binaryMessage.Data));
                }
            }
        }
    }
}
