using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using LeanCloud;

namespace LeanCloud.Test {
    public class UserTest {
        [SetUp]
        public void SetUp() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz",
                ApplicationKey = "pbf6Nk5seyjilexdpyrPwjSp",
                ApiServer = "https://avoscloud.com"
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }

        [Test]
        public async Task Register() {
            AVUser user = new AVUser {
                Username = $"hello_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Password = "world"
            };
            await user.SignUpAsync();
            TestContext.Out.WriteLine($"{user.ObjectId} registered");
        }

        [Test]
        public async Task LoginWithUsername() {
            AVUser user = await AVUser.LogInAsync("hello", "111111");
            TestContext.Out.WriteLine($"{user.ObjectId}, {user.SessionToken} login");
        }

        [Test]
        public async Task LoginWithEmail() {
            AVUser user = await AVUser.LogInWithEmailAsync("111111@qq.com", "111111");
            Assert.AreEqual(user, AVUser.CurrentUser);
            TestContext.Out.WriteLine($"{AVUser.CurrentUser.SessionToken} login");
        }

        [Test]
        public async Task Become() {
            AVUser user = await AVUser.BecomeAsync("o8onm9bq8z127lz837mi6qhcg");
            Assert.AreEqual(user, AVUser.CurrentUser);
            TestContext.Out.WriteLine($"{AVUser.CurrentUser.SessionToken} login");
        }

        [Test]
        public async Task IsAuthenticated() {
            AVUser user = await AVUser.LogInWithEmailAsync("111111@qq.com", "111111");
            Assert.IsTrue(user.IsCurrent);
            Assert.AreEqual(user, AVUser.CurrentUser);
            bool authenticated = await user.IsAuthenticatedAsync();
            Assert.IsTrue(authenticated);
        }

        [Test]
        public async Task RefreshSessionToken() {
            AVUser user = await AVUser.LogInWithEmailAsync("111111@qq.com", "111111");
            Assert.IsTrue(user.IsCurrent);
            await user.RefreshSessionTokenAsync();
            TestContext.Out.WriteLine(user.SessionToken);
        }

        [Test]
        public async Task UpdatePassword() {
            AVUser user = await AVUser.LogInAsync("111111", "111111");
            await user.UpdatePasswordAsync("111111", "222222");
            await user.UpdatePasswordAsync("222222", "111111");
        }

        [Test]
        public async Task LoginWithAuthData() {
            AVUser user = await AVUser.LogInWithAuthDataAsync(new Dictionary<string, object> {
                { "openid", "0395BA18A5CD6255E5BA185E7BEBA242" },
                { "access_token", "12345678-SaMpLeTuo3m2avZxh5cjJmIrAfx4ZYyamdofM7IjU" },
                { "expires_in", 1382686496 }
            }, "qq");
            Assert.NotNull(user.SessionToken);
            TestContext.Out.WriteLine(user.SessionToken);
        }

        [Test]
        public async Task AssociateAuthData() {
            AVUser user = await AVUser.LogInAsync("111111", "111111");
            Assert.NotNull(user.SessionToken);
            await user.AssociateAuthDataAsync(new Dictionary<string, object> {
                { "openid", "0395BA18A5CD6255E5BA185E7BEBA243" },
                { "access_token", "12345678-SaMpLeTuo3m2avZxh5cjJmIrAfx4ZYyamdofM7IjU" },
                { "expires_in", 1382686496 }
            }, "qq");
        }

        [Test]
        public async Task Anonymously() {
            AVUser user = await AVUser.LogInAnonymouslyAsync();
            Assert.NotNull(user.SessionToken);
            TestContext.Out.WriteLine(user.SessionToken);
        }

        [Test]
        public async Task GetRoles() {
            AVUser user = await AVUser.LogInAsync("111111", "111111");
            Assert.NotNull(user.SessionToken);
            IEnumerable<AVRole> roles = await user.GetRolesAsync();
            Assert.Greater(roles.Count(), 0);
            foreach (AVRole role in roles) {
                TestContext.Out.WriteLine(role.Name);
            }
        }
    }
}
