using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LeanCloud;
using LeanCloud.Realtime;

using static NUnit.Framework.TestContext;

namespace Realtime.Test {
    public class Signature {
        internal const string AppId = "7oDgNicekFVXBMkRKcLpvX5w-gzGzoHsz";
        internal const string AppKey = "tPT17REZjS3DfjTJodw6fJzj";
        internal const string AppServer = "https://7odgnice.lc-cn-n1-shared.com";

        private LCIMClient hello;
        private LCIMClient world;
        private LCIMClient client;

        private LCIMConversation conversation;

        [SetUp]
        public async Task SetUp() {
            LCLogger.LogDelegate += Print;
            LCApplication.Initialize(AppId, AppKey, AppServer);

            hello = new LCIMClient("hello", signatureFactory: new LocalSignatureFactory());
            world = new LCIMClient("world", signatureFactory: new LocalSignatureFactory());
            client = new LCIMClient(Guid.NewGuid().ToString(), signatureFactory: new LocalSignatureFactory());

            await hello.Open();
            await world.Open();
            await client.Open();
        }

        [TearDown]
        public async Task TearDown() {
            await hello.Close();
            await world.Close();
            await client.Close();
            LCLogger.LogDelegate -= Print;
        }

        [Test]
        [Order(1)]
        public async Task CreateConversation() {
            conversation = await hello.CreateConversation(new string[] { "world" });
        }

        [Test]
        [Order(2)]
        public async Task OperateConversation() {
            // 添加成员
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            
            client.OnInvited = (conv, initBy) => {
                WriteLine($"{client.Id} is invited by {initBy}");
                tcs.SetResult(null);
            };

            try {
                await conversation.AddMembers(new string[] { client.Id });
                Assert.AreEqual(conversation.MemberIds.Count, 3);

                await conversation.MuteMembers(new string[] { client.Id });
                Assert.True(conversation.MutedMemberIds.Contains(client.Id));

                LCIMPageResult result = await conversation.QueryMutedMembers();
                Assert.True(result.Results.Contains(client.Id));

                await conversation.UnmuteMembers(new string[] { client.Id });
                Assert.Zero(conversation.MutedMemberIds.Count);

                await conversation.RemoveMembers(new string[] { client.Id });
                Assert.AreEqual(conversation.MemberIds.Count, 2);
            } catch (LCException e) {
                if (e.Code == 4325) {
                    tcs.TrySetResult(null);
                } else {
                    throw e;
                }
            }

            await tcs.Task;
        }

        [Test]
        [Order(3)]
        public async Task BlockMember() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            bool f1 = false, f2 = false;
            hello.OnMembersBlocked = (conv, blockedMembers, initBy) => {
                Assert.True(blockedMembers.Contains(client.Id));
                f1 = true;
                if (f1 && f2) {
                    tcs.SetResult(null);
                }
            };
            client.OnBlocked = (conv, initBy) => {
                WriteLine($"{client.Id} is blocked by {initBy}");
                f2 = true;
                if (f1 && f2) {
                    tcs.TrySetResult(null);
                }
            };
            try {
                await conversation.BlockMembers(new string[] { client.Id });
                LCIMPageResult result = await conversation.QueryBlockedMembers();
                Assert.True(result.Results.Contains(client.Id));
            } catch (LCException e) {
                if (e.Code == 4544) {
                    tcs.TrySetResult(null);
                } else {
                    throw e;
                }
            }

            await tcs.Task;
        }

        internal static void Print(LCLogLevel level, string info) {
            switch (level) {
                case LCLogLevel.Debug:
                    TestContext.Out.WriteLine($"[DEBUG] {info}");
                    break;
                case LCLogLevel.Warn:
                    TestContext.Out.WriteLine($"[WARNING] {info}");
                    break;
                case LCLogLevel.Error:
                    TestContext.Out.WriteLine($"[ERROR] {info}");
                    break;
                default:
                    TestContext.Out.WriteLine(info);
                    break;
            }
        }
    }
}
