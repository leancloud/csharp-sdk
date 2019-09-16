using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using LeanCloud;

namespace LeanCloudTests {
    public class QueryTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task BasicQuery() {
            var query = new AVQuery<AVObject>("Account");
            query.WhereGreaterThanOrEqualTo("balance", 100);
            query.WhereLessThanOrEqualTo("balance", 100);
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

        [Test]
        public async Task Related() {
            AVObject todo = AVObject.CreateWithoutData("Todo", "5d71f798d5de2b006c0136bc");
            AVQuery<AVObject> query = new AVQuery<AVObject>("Tag");
            query.WhereRelatedTo(todo, "tags");
            IEnumerable<AVObject> results = await query.FindAsync();
            foreach (AVObject tag in results) {
                TestContext.Out.WriteLine(tag.ObjectId);
            }
        }

        [Test]
        public void Where() {
            AVQuery<AVObject> q1 = new AVQuery<AVObject>();
            q1.WhereEqualTo("aa", "bb");
            AVQuery<AVObject> q2 = new AVQuery<AVObject>();
            q2.WhereEqualTo("cc", "dd");
            q2.WhereEqualTo("ee", "ff");
            List<AVQuery<AVObject>> queryList = new List<AVQuery<AVObject>> {
                q1, q2
            };
            AVQuery<AVObject> query = AVQuery<AVObject>.Or(queryList);
            IDictionary<string, object> obj = query.BuildWhere();
            TestContext.Out.WriteLine(JsonConvert.SerializeObject(obj));

            AVQuery<AVObject> q3 = new AVQuery<AVObject>();
            q3.WhereEqualTo("xx", "yy");
            IDictionary<string, object> q3Obj = q3.BuildWhere();
            TestContext.Out.WriteLine(JsonConvert.SerializeObject(q3Obj));

            AVQuery<AVObject> q4 = new AVQuery<AVObject>();
            q4.WhereEqualTo("aaa", "bbb");
            q4.WhereEqualTo("ccc", "ddd");
            IDictionary<string, object> q4Obj = q4.BuildWhere();
            TestContext.Out.WriteLine(JsonConvert.SerializeObject(q4Obj));

            AVQuery<AVObject> q5 = new AVQuery<AVObject>();
            q5.WhereEqualTo("aaa", "bbb");
            q5.WhereEqualTo("aaa", "ccc");
            IDictionary<string, object> q5Obj = q5.BuildWhere();
            TestContext.Out.WriteLine(JsonConvert.SerializeObject(q5Obj));
        }
    }
}
