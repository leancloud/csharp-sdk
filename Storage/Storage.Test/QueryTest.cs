using NUnit.Framework;
using System;
using System.Threading.Tasks;
using LeanCloud;

namespace LeanCloudTests {
    public class QueryTest {
        [SetUp]
        public void SetUp() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz",
                ApplicationKey = "pbf6Nk5seyjilexdpyrPwjSp",
                RTMServer = "https://router-g0-push.avoscloud.com",
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }

        [Test]
        public async Task TestQuery() {
            var query = new AVQuery<AVObject>("Foo");
            query.WhereEqualTo("content", "hello, world");
            var count = await query.CountAsync();
            Assert.Greater(count, 8);
        }
    }
}
