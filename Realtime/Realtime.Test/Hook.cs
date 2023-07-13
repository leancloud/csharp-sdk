using NUnit.Framework;
using System;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Realtime;

namespace Realtime.Test {
    public class Hook : BaseTest {
        private static readonly string FORBIDDEN_RTM_USER = "forbidden_rtm_user";
        private static readonly string MUTE_RTM_USER = "mute_rtm_user";

        [Test]
        [Timeout(10000)]
        public async Task ForbidCreatingConv() {
            LCIMClient client = new LCIMClient(FORBIDDEN_RTM_USER);
            await client.Open();
            try {
                await client.CreateConversation(new string[] { "world" },
                    name: Guid.NewGuid().ToString(), unique: false);
            } catch (LCException ex) {
                Console.WriteLine(ex.Code);
                Assert.AreEqual(ex.Code, 4305);
            } finally {
                await client.Close();
            }
        }

        [Test]
        [Timeout(10000)]
        public async Task MuteUser() {
            LCIMClient client = new LCIMClient(MUTE_RTM_USER);
            await client.Open();
            try {
                LCIMConversation conv = await client.CreateConversation(new string[] { "world" },
                    name: Guid.NewGuid().ToString(), unique: false);
                LCIMTextMessage msg = new LCIMTextMessage("hello");
                await conv.Send(msg);
            } catch (LCException ex) {
                Console.WriteLine(ex.Code);
            } finally {
                await client.Close();
            }
        }
    }
}
