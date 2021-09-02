using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Realtime;

using static NUnit.Framework.TestContext;

namespace Realtime.Test {
    public class Conversation {
        private LCIMClient c1;
        private LCIMClient c2;
        private LCIMClient lean;
        private LCIMConversation conversation;

        [SetUp]
        public async Task SetUp() {
            Utils.SetUp();
            c1 = new LCIMClient(Guid.NewGuid().ToString());
            await c1.Open();
            c2 = new LCIMClient(Guid.NewGuid().ToString());
            await c2.Open();
            lean = new LCIMClient("lean");
            await lean.Open();
            conversation = await c1.CreateConversation(new string[] { "lean", "cloud" });
        }

        [TearDown]
        public async Task TearDown() {
            await c1.Close();
            await c2.Close();
            await lean.Close();
            Utils.TearDown();
        }

        [Test]
        [Order(0)]
        public async Task AddMember() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            c2.OnInvited = (conv, initBy) => {
                WriteLine($"{c2.Id} is invited by {initBy}");
                tcs.SetResult(null);
            };
            await conversation.AddMembers(new string[] { c2.Id });
            Assert.AreEqual(conversation.MemberIds.Count, 4);
            await tcs.Task;
        }

        [Test]
        [Order(1)]
        public async Task MuteMembers() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            bool f1 = false, f2 = false;
            c1.OnMembersMuted = (conv, mutedMembers, initBy) => {
                Assert.True(mutedMembers.Contains(lean.Id));
                Assert.True(conversation.MutedMemberIds.Contains(lean.Id));
                f1 = true;
                if (f1 && f2) {
                    tcs.TrySetResult(null);
                }
            };
            lean.OnMuted = (conv, initBy) => {
                WriteLine($"{lean.Id} is muted by {initBy}");
                f2 = true;
                if (f1 && f2) {
                    tcs.TrySetResult(null);
                }
            };
            try {
                await conversation.MuteMembers(new string[] { "lean" });
                Assert.True(conversation.MutedMemberIds.Contains("lean"));
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
        [Order(2)]
        public async Task UnmuteMembers() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            bool f1 = false, f2 = false;
            c1.OnMembersUnmuted = (conv, unmutedMembers, initBy) => {
                Assert.True(unmutedMembers.Contains(lean.Id));
                Assert.False(conversation.MutedMemberIds.Contains(lean.Id));
                f1 = true;
                if (f1 && f2) {
                    tcs.SetResult(null);
                }
            };
            lean.OnUnmuted = (conv, initBy) => {
                WriteLine($"{lean.Id} is unmuted by {initBy}");
                f2 = true;
                if (f1 && f2) {
                    tcs.SetResult(null);
                }
            };
            try {
                await conversation.UnmuteMembers(new string[] { "lean" });
                Assert.False(conversation.MutedMemberIds.Contains("lean"));
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
        public async Task BlockMembers() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            bool f1 = false, f2 = false;
            c1.OnMembersBlocked = (conv, blockedMembers, initBy) => {
                Assert.True(blockedMembers.Contains(lean.Id));
                f1 = true;
                if (f1 && f2) {
                    tcs.SetResult(null);
                }
            };
            lean.OnBlocked = (conv, initBy) => {
                WriteLine($"{lean.Id} is blocked by {initBy}");
                f2 = true;
                if (f1 && f2) {
                    tcs.TrySetResult(null);
                }
            };
            try {
                await conversation.BlockMembers(new string[] { "lean" });
                LCIMPageResult result = await conversation.QueryBlockedMembers();
                Assert.True(result.Results.Contains("lean"));
            } catch (LCException e) {
                if (e.Code == 4544) {
                    tcs.TrySetResult(null);
                } else {
                    throw e;
                }
            }
            
            await tcs.Task;
        }

        [Test]
        [Order(4)]
        public async Task UnblockMembers() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            bool f1 = false, f2 = false;
            c1.OnMembersUnblocked = (conv, blockedMembers, initBy) => {
                Assert.True(blockedMembers.Contains(lean.Id));
                f1 = true;
                if (f1 && f2) {
                    tcs.SetResult(null);
                }
            };
            lean.OnUnblocked = (conv, initBy) => {
                WriteLine($"{lean.Id} is unblocked by {initBy}");
                f2 = true;
                if (f1 && f2) {
                    tcs.SetResult(null);
                }
            };
            try {
                await conversation.UnblockMembers(new string[] { "lean" });
                LCIMPageResult result = await conversation.QueryBlockedMembers();
                Assert.False(result.Results.Contains("lean"));
            } catch (LCException e) {
                if (e.Code == 4544) {
                    tcs.TrySetResult(null);
                } else {
                    throw e;
                }
            }
            
            await tcs.Task;
        }

        [Test]
        [Order(5)]
        public async Task UpdateRole() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            c1.OnMemberInfoUpdated = (conv, member, role, initBy) => {
                WriteLine($"{member} is {role} by {initBy}");
                tcs.TrySetResult(null);
            };
            try {
                await conversation.UpdateMemberRole("cloud", LCIMConversationMemberInfo.Manager);
                LCIMConversationMemberInfo memberInfo = await conversation.GetMemberInfo("cloud");
                Assert.True(memberInfo.IsManager);
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
        [Order(6)]
        public async Task RemoveMember() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            c2.OnKicked = (conv, initBy) => {
                WriteLine($"{c2.Id} is kicked by {initBy}");
                tcs.SetResult(null);
            };
            await conversation.RemoveMembers(new string[] { c2.Id });
            Assert.AreEqual(conversation.MemberIds.Count, 3);
            await tcs.Task;
        }

        [Test]
        [Order(7)]
        [Timeout(60000)]
        public async Task UpdateInfo() {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            lean.OnConversationInfoUpdated = (conv, attrs, initBy) => {
                WriteLine(attrs);
                Assert.AreEqual(conv.Name, "leancloud");
                Assert.AreEqual(conv["k1"], "v1");
                Assert.AreEqual(conv["k2"], "v2");
                Assert.AreEqual(attrs["k1"], "v1");
                Assert.AreEqual(attrs["k2"], "v2");
                tcs.TrySetResult(null);
            };
            
            await Task.Delay(5000);

            await conversation.UpdateInfo(new Dictionary<string, object> {
                { "name", "leancloud" },
                { "k1", "v1" },
                { "k2", "v2" }
            });
            Assert.AreEqual(conversation.Name, "leancloud");
            Assert.AreEqual(conversation["k1"], "v1");
            Assert.AreEqual(conversation["k2"], "v2");

            //await tcs.Task;
        }
    }
}
