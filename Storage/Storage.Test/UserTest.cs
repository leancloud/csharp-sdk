using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using LeanCloud;

namespace LeanCloudTests {
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
            AVUser user = await AVUser.LogInByEmailAsync("111111@qq.com", "111111");
            Assert.AreEqual(user, AVUser.CurrentUser);
            TestContext.Out.WriteLine($"{AVUser.CurrentUser.SessionToken} login");
        }

        [Test]
        public async Task Become() {
            AVUser user = await AVUser.BecomeAsync("36idbfnt8hlmdo4rki0f5hevq");
            Assert.AreEqual(user, AVUser.CurrentUser);
            TestContext.Out.WriteLine($"{AVUser.CurrentUser.SessionToken} login");
        }

        [Test]
        public async Task IsAuthenticated() {
            AVUser user = await AVUser.LogInByEmailAsync("111111@qq.com", "111111");
            Assert.IsTrue(user.IsCurrent);
            Assert.AreEqual(user, AVUser.CurrentUser);
            bool authenticated = await user.IsAuthenticatedAsync();
            Assert.IsTrue(authenticated);
        }

        [Test]
        public async Task RefreshSessionToken() {
            AVUser user = await AVUser.LogInByEmailAsync("111111@qq.com", "111111");
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
    }
}
