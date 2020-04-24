using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Common;
using LeanCloud.Realtime;

namespace Realtime.Test {
    public class Conversation {
        private LCIMClient c1;
        private LCIMClient c2;
        private LCIMConversation conversation;

        [SetUp]
        public async Task SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
            c1 = new LCIMClient(Guid.NewGuid().ToString());
            await c1.Open();
            c2 = new LCIMClient(Guid.NewGuid().ToString());
            await c2.Open();
            conversation = await c1.CreateConversation(new string[] { "lean", "cloud" });
        }

        [TearDown]
        public async Task TearDown() {
            await c1.Close();
            await c2.Close();
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        [Order(0)]
        public async Task AddMember() {
            await conversation.AddMembers(new string[] { c2.Id });
            Assert.AreEqual(conversation.MemberIds.Count, 4);
        }

        [Test]
        [Order(1)]
        public async Task MuteMembers() {
            await conversation.MuteMembers(new string[] { "lean" });
            Assert.True(conversation.MutedMemberIds.Contains("lean"));
        }

        [Test]
        [Order(2)]
        public async Task UnmuteMembers() {
            await conversation.UnmuteMembers(new string[] { "lean" });
            Assert.False(conversation.MutedMemberIds.Contains("lean"));
        }

        [Test]
        [Order(3)]
        public async Task BlockMembers() {
            await conversation.BlockMembers(new string[] { "lean" });
            LCIMPageResult result = await conversation.QueryBlockedMembers();
            Assert.True(result.Results.Contains("lean"));
        }

        [Test]
        [Order(4)]
        public async Task UnblockMembers() {
            await conversation.UnblockMembers(new string[] { "lean" });
            LCIMPageResult result = await conversation.QueryBlockedMembers();
            Assert.False(result.Results.Contains("lean"));
        }

        [Test]
        [Order(5)]
        public async Task UpdateRole() {
            await conversation.UpdateMemberRole("cloud", LCIMConversationMemberInfo.Manager);
            LCIMConversationMemberInfo memberInfo = await conversation.GetMemberInfo("cloud");
            Assert.True(memberInfo.IsManager);
        }

        [Test]
        [Order(6)]
        public async Task RemoveMember() {
            await conversation.RemoveMembers(new string[] { c2.Id });
            Assert.AreEqual(conversation.MemberIds.Count, 3);
        }

        [Test]
        [Order(7)]
        public async Task UpdateInfo() {
            await conversation.UpdateInfo(new Dictionary<string, object> {
                { "name", "leancloud" },
                { "k1", "v1" },
                { "k2", "v2" }
            });
            Assert.AreEqual(conversation.Name, "leancloud");
            Assert.AreEqual(conversation["k1"], "v1");
            Assert.AreEqual(conversation["k2"], "v2");
        }
    }
}
