using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class FriendTest : BaseTest {
        async Task<LCUser> SignUp() {
            LCUser user = new LCUser {
                Username = Guid.NewGuid().ToString(),
                Password = "world"
            };
            return await user.SignUp();
        }

        async Task<LCFriendshipRequest> GetRequest() {
            LCUser user = await LCUser.GetCurrent();
            LCQuery<LCFriendshipRequest> query = new LCQuery<LCFriendshipRequest>("_FriendshipRequest")
                .WhereEqualTo("friend", user)
                .WhereEqualTo("status", "pending");
            return await query.First();
        }

        async Task<ReadOnlyCollection<LCObject>> GetFriends() {
            LCUser user = await LCUser.GetCurrent();
            LCQuery<LCObject> query = new LCQuery<LCObject>("_Followee")
                .WhereEqualTo("user", user)
                .WhereEqualTo("friendStatus", true);
            return await query.Find();
        }

        private LCUser user1;
        private LCUser user2;

        [Test]
        [Order(0)]
        public async Task Init() {
            user1 = await SignUp();
            user2 = await SignUp();
            Dictionary<string, object> attrs = new Dictionary<string, object> {
                { "group", "sport" }
            };
            await LCFriendship.Request(user1.ObjectId, attrs);

            await SignUp();
            await LCFriendship.Request(user1.ObjectId);

            await LCUser.BecomeWithSessionToken(user1.SessionToken);
        }

        [Test]
        [Order(1)]
        public async Task Accept() {
            // 查询好友请求
            LCFriendshipRequest request = await GetRequest();
            LCUser user = await LCUser.GetCurrent();
            Assert.AreEqual(request.Friend.ObjectId, user.ObjectId);
            Assert.AreEqual(request.Status, "pending");
            // 接受
            Dictionary<string, object> attrs = new Dictionary<string, object> {
                { "group", "sport" }
            };
            await LCFriendship.AcceptRequest(request, attrs);
            // 查询好友
            ReadOnlyCollection<LCObject> friends = await GetFriends();
            Assert.Greater(friends.Count, 0);
            foreach (LCObject friend in friends) {
                Assert.AreEqual(friend["group"], "sport");
            }
        }

        [Test]
        [Order(2)]
        public async Task Decline() {
            // 查询好友请求
            LCFriendshipRequest request = await GetRequest();
            // 拒绝
            await LCFriendship.DeclineRequest(request);
        }

        [Test]
        [Order(3)]
        public async Task Attributes() {
            LCObject followee = (await GetFriends()).First();
            followee["group"] = "friend";
            await followee.Save();
        }

        [Test]
        [Order(4)]
        public async Task Delete() {
            await user1.Unfollow(user2.ObjectId);
            // 查询好友
            Assert.AreEqual((await GetFriends()).Count, 0);
        }
    }
}
