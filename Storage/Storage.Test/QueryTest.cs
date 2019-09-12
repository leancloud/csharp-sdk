using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud;

namespace LeanCloudTests {
    public class QueryTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task BasicQuery() {
            var query = new AVQuery<AVObject>("Foo");
            query.WhereGreaterThanOrEqualTo("a", 100);
            query.WhereLessThanOrEqualTo("a", 100);
            //query.WhereEqualTo("content", "hello");
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

        [Test]
        public async Task OrPro() {
            AVQuery<AVObject> q1 = AVQuery<AVObject>.Or(new List<AVQuery<AVObject>> {
                new AVQuery<AVObject>("Account").WhereEqualTo("balance", 100)
            });
            AVQuery<AVObject> q2 = AVQuery<AVObject>.Or(new List<AVQuery<AVObject>> {
                new AVQuery<AVObject>("Account").WhereEqualTo("balance", 200)
            });
            AVQuery<AVObject> query = AVQuery<AVObject>.Or(new List<AVQuery<AVObject>> {
                q1, q2
            });
            query.WhereEqualTo("balance", 100);
            IEnumerable<AVObject> results = await query.FindAsync();
            foreach (AVObject result in results) {
                TestContext.Out.WriteLine(result.ObjectId);
            }
        }
    }
}
