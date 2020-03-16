using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Realtime;

namespace Realtime.Test {
    public class Message {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task Send() {
            try {
                string clientId = Guid.NewGuid().ToString();
                LCIMClient client = new LCIMClient(clientId);
                await client.Open();
                List<string> memberIdList = new List<string> { "world" };
                string name = Guid.NewGuid().ToString();
                LCIMConversation conversation = await client.CreateConversation(memberIdList, name: name, unique: false);
                LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
                await conversation.Send(textMessage);

                TestContext.WriteLine(textMessage.Id);
                TestContext.WriteLine(textMessage.DeliveredAt);
                Assert.NotNull(textMessage.Id);
            } catch (Exception e) {
                LCLogger.Error(e.Message);
            }
        }
    }
}
