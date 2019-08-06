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
                ApiServer = "https://avoscloud.com"
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }

        [Test]
        public async Task TestQuery() {
            var query = new AVQuery<AVObject>("Foo");
            query.WhereEqualTo("content", "hello");
            var results = await query.FindAsync();
            foreach (var result in results) {
                TestContext.Out.WriteLine(result.ObjectId);
            }
        }

        [Test]
        public async Task TestQueryCount() {
            var query = new AVQuery<AVObject>("Foo");
            query.WhereEqualTo("content", "hello, world");
            var count = await query.CountAsync();
            Assert.Greater(count, 8);
        }
    }
}
