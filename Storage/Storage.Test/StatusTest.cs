using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class StatusTest {
        private LCUser user1;
        private LCUser user2;
        private LCUser user3;

        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo",
                "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        [Order(0)]
        public async Task Init() {
            user1 = new LCUser {
                Username = Guid.NewGuid().ToString(),
                Password = "world"
            };
            await user1.SignUp();

            user2 = new LCUser {
                Username = Guid.NewGuid().ToString(),
                Password = "world"
            };
            await user2.SignUp();

            user3 = new LCUser {
                Username = Guid.NewGuid().ToString(),
                Password = "world"
            };
            await user3.SignUp();
        }

        [Test]
        [Order(1)]
        public async Task Follow() {
            await LCUser.BecomeWithSessionToken(user2.SessionToken);
            Dictionary<string, object> attrs = new Dictionary<string, object> {
                { "score", 100 }
            };
            await user2.Follow(user1.ObjectId, attrs);

            await LCUser.BecomeWithSessionToken(user3.SessionToken);
            await user3.Follow(user2.ObjectId);
        }

        [Test]
        [Order(2)]
        public async Task QueryFollowersAndFollowees() {
            await LCUser.BecomeWithSessionToken(user2.SessionToken);

            LCQuery<LCObject> query = user2.FolloweeQuery();
            ReadOnlyCollection<LCObject> results = await query.Find();
            Assert.Greater(results.Count, 0);
            foreach (LCObject item in results) {
                Assert.IsTrue(item["followee"] is LCObject);
                Assert.AreEqual(user1.ObjectId, (item["followee"] as LCObject).ObjectId);
            }

            query = user2.FollowerQuery();
            results = await query.Find();
            Assert.Greater(results.Count, 0);
            foreach (LCObject item in results) {
                Assert.IsTrue(item["follower"] is LCObject);
                Assert.AreEqual(user3.ObjectId, (item["follower"] as LCObject).ObjectId);
            }

            LCFollowersAndFollowees followersAndFollowees = await user2.GetFollowersAndFollowees(true, true, true);
            Assert.AreEqual(followersAndFollowees.FollowersCount, 1);
            Assert.AreEqual(followersAndFollowees.FolloweesCount, 1);
        }

        [Test]
        [Order(3)]
        public async Task Send() {
            await LCUser.BecomeWithSessionToken(user1.SessionToken);

            // 给粉丝发送状态
            LCStatus status = new LCStatus {
                Data = new Dictionary<string, object> {
                    { "image", "xxx.jpg" },
                    { "content", "hello, world" }
                }
            };
            await LCStatus.SendToFollowers(status);

            // 给某个用户发送私信
            LCStatus privateStatus = new LCStatus {
                Data = new Dictionary<string, object> {
                    { "image", "xxx.jpg" },
                    { "content", "hello, game" }
                }
            };
            await LCStatus.SendPrivately(privateStatus, user2.ObjectId);
        }

        [Test]
        [Order(4)]
        public async Task Query() {
            await Task.Delay(5000);
            await LCUser.BecomeWithSessionToken(user2.SessionToken);

            LCStatusCount statusCount = await LCStatus.GetCount(LCStatus.InboxTypeDefault);
            Assert.Greater(statusCount.Total, 0);
            LCStatusCount privateCount = await LCStatus.GetCount(LCStatus.InboxTypePrivate);
            Assert.Greater(privateCount.Total, 0);

            LCStatusQuery query = new LCStatusQuery(LCStatus.InboxTypeDefault);
            ReadOnlyCollection<LCStatus> statuses = await query.Find();
            foreach (LCStatus status in statuses) {
                Assert.AreEqual((status["source"] as LCObject).ObjectId, user1.ObjectId);
                await status.Delete();
            }

            await LCStatus.ResetUnreadCount(LCStatus.InboxTypePrivate);
        }

        [Test]
        [Order(5)]
        public async Task Unfollow() {
            await LCUser.BecomeWithSessionToken(user2.SessionToken);
            await user2.Unfollow(user1.ObjectId);

            await LCUser.BecomeWithSessionToken(user3.SessionToken);
            await user3.Unfollow(user1.ObjectId);
        }
    }
}
