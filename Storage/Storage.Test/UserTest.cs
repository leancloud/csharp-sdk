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
        public async Task Login() {
            AVUser user = await AVUser.LogInAsync("hello", "world");
            TestContext.Out.WriteLine($"{user.ObjectId} login");
        }
    }
}
