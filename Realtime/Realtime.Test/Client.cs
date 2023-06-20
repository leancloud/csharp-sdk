using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Realtime;
using LeanCloud.Storage;

using static NUnit.Framework.TestContext;

namespace Realtime.Test {
    public class Client : BaseTest {
        private const string USERNAME1 = "username1";
        private const string PASSWORD1 = "password1";

        private const string USERNAME2 = "username2";
        private const string PASSWORD2 = "password2";

        [SetUp]
        public override async Task SetUp() {
            await base.SetUp();
            await NewUser(USERNAME1, PASSWORD1);
            await NewUser(USERNAME2, PASSWORD2);
        }

        [Test]
        public async Task OpenAndClose() {
            LCIMClient c1 = new LCIMClient("c1");
            LCIMClient c2 = new LCIMClient("c2");
            await c1.Open();
            await c2.Open();

            await c1.Close();
            await c2.Close();
        }

        [Test]
        public async Task OpenAndCloseByLCUser() {
            LCUser user = await LCUser.Login(USERNAME1, PASSWORD1);
            LCIMClient client = new LCIMClient(user);
            await client.Open();


            LCUser game = await LCUser.Login(USERNAME2, PASSWORD2);
            LCIMClient client2 = new LCIMClient(game);
            await client2.Open();

            await client.Close();
            await client2.Close();
        }

        [Test]
        public async Task CreateConversation() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            string clientId = Guid.NewGuid().ToString();
            LCIMClient client = new LCIMClient(clientId);

            await client.Open();

            client.OnInvited = (conv, initBy) => {
                WriteLine($"on invited: {initBy}");
                WriteLine(conv.CreatorId);
            };

            string name = Guid.NewGuid().ToString();

            client.OnMembersJoined = (conv, memberList, initBy) => {
                WriteLine($"custom id: {conv["custom_id"]}");
                Assert.AreEqual(conv["custom_id"], name);
                WriteLine($"on members joined: {initBy}");
                foreach (string memberId in conv.MemberIds) {
                    WriteLine(memberId);
                }
                tcs.SetResult(null);
            };

            Dictionary<string, object> props = new Dictionary<string, object> {
                { "custom_id", name }
            };
            LCIMConversation conversation = await client.CreateConversation(new string[] { "world" },
                name: name, unique: false, properties: props);

            await tcs.Task;
        }

        [Test]
        [Timeout(10000)]
        public async Task CreateChatRoom() {
            string clientId = Guid.NewGuid().ToString();
            LCIMClient client = new LCIMClient(clientId);

            await client.Open();

            client.OnInvited = (conv, initBy) => {
                WriteLine($"on invited: {initBy}");
            };

            string name = Guid.NewGuid().ToString();
            LCIMConversation conversation = await client.CreateChatRoom(name);

            string visitorId = Guid.NewGuid().ToString();
            LCIMClient visitor = new LCIMClient(visitorId);

            await visitor.Open();

            LCIMChatRoom chatRoom = await visitor.GetConversation(conversation.Id) as LCIMChatRoom;
            await chatRoom.Join();

            LCIMTextMessage textMessage = new LCIMTextMessage("hello, world");
            await conversation.Send(textMessage);

            int count = await chatRoom.GetMembersCount();
            int onlineCount = await chatRoom.GetOnlineMembersCount();
            Assert.AreEqual(count, onlineCount);

            Exception ex = Assert.ThrowsAsync<Exception>(async () => {
                await chatRoom.AddMembers(new string[] { "test" });
            });
            Assert.AreEqual(ex.Message, "Add members is not allowed in chat room.");

            ReadOnlyCollection<string> onlineMembers = await chatRoom.GetOnlineMembers();
            Assert.GreaterOrEqual(onlineMembers.Count, 1);
            foreach (string memberId in onlineMembers) {
                WriteLine($"{memberId} online");
            }

            await client.Close();
            await visitor.Close();
        }

        [Test]
        public async Task CreateTemporaryConversation() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            string clientId = Guid.NewGuid().ToString();
            LCIMClient client = new LCIMClient(clientId);

            await client.Open();

            client.OnInvited = (conv, initBy) => {
                WriteLine($"on invited: {initBy}");
            };

            client.OnMembersJoined = (conv, memberList, initBy) => {
                WriteLine($"on members joined: {initBy}");
                tcs.SetResult(null);
            };

            LCIMTemporaryConversation temporaryConversation = await client.CreateTemporaryConversation(new string[] { "world" });
            Assert.False(temporaryConversation.IsExpired);

            await tcs.Task;
        }

        private async Task NewUser(string username, string password) {
            try {
                await LCUser.Login(username, password);
            } catch (LCException e) {
                if (e.Code == 211) {
                    LCUser user1 = new LCUser {
                        Username = username,
                        Password = password
                    };
                    await user1.SignUp();
                } else {
                    throw e;
                }
            }
        }

        [Test]
        [Timeout(30000)]
        public async Task Reconnect() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            string clientId = Guid.NewGuid().ToString();
            LCIMClient client = new LCIMClient(clientId);

            await client.Open();

            client.OnPaused = () => {
                LCLogger.Debug("paused.");
            };
            client.OnResume = () => {
                LCLogger.Debug("resumed.");
                tcs.TrySetResult(null);
            };

            await Task.Delay(3000);
            LCRealtime.Pause();
            await Task.Delay(3000);
            LCRealtime.Resume();

            await tcs.Task;
        }

        // 太耗时了
        //[Test]
        //[Timeout(200000)]
        //public async Task PingPong() {
        //    string clientId = Guid.NewGuid().ToString();
        //    LCIMClient client = new LCIMClient(clientId);

        //    await client.Open();

        //    await Task.Delay(190 * 1000);
        //}
    }
}
