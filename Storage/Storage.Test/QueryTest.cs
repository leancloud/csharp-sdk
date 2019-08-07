using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
        public async Task BasicQuery() {
            var query = new AVQuery<AVObject>("Foo");
            query.WhereEqualTo("content", "hello");
            var results = await query.FindAsync();
            foreach (var result in results) {
                TestContext.Out.WriteLine(result.ObjectId);
            }
        }

        [Test]
        public async Task Count() {
            var query = new AVQuery<AVObject>("Foo");
            query.WhereEqualTo("content", "hello, world");
            var count = await query.CountAsync();
            Assert.Greater(count, 8);
        }

        [Test]
        public async Task Or() {
            AVQuery<AVObject> q1 = new AVQuery<AVObject>("Foo");
            q1.WhereEqualTo("content", "hello");
            AVQuery<AVObject> q2 = new AVQuery<AVObject>("Foo");
            q2.WhereEqualTo("content", "world");
            AVQuery<AVObject> query = AVQuery<AVObject>.Or(new List<AVQuery<AVObject>> { q1, q2 });
            IEnumerable<AVObject> results = await query.FindAsync();
            foreach (AVObject result in results) {
                TestContext.Out.WriteLine(result.ObjectId);
            }
        }

        [Test]
        public async Task And() {
            AVQuery<AVObject> q1 = new AVQuery<AVObject>("Foo");
            q1.WhereContains("content", "hello");
            AVQuery<AVObject> q2 = new AVQuery<AVObject>("Foo");
            q2.WhereContains("content", "world");
            AVQuery<AVObject> query = AVQuery<AVObject>.And(new List<AVQuery<AVObject>> { q1, q2 });
            IEnumerable<AVObject> results = await query.FindAsync();
            TestContext.Out.WriteLine($"Count: {results.Count()}");
            foreach (AVObject result in results) {
                TestContext.Out.WriteLine(result.ObjectId);
            }
        }
    }
}
