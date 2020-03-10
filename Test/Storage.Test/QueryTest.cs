using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage;
using LeanCloud.Common;

namespace LeanCloud.Test {
    public class QueryTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task BaseQuery() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
            query.Limit(2);
            List<LCObject> list = await query.Find();
            TestContext.WriteLine(list.Count);
            Assert.AreEqual(list.Count, 2);

            foreach (LCObject item in list) {
                Assert.NotNull(item.ClassName);
                Assert.NotNull(item.ObjectId);
                Assert.NotNull(item.CreatedAt);
                Assert.NotNull(item.UpdatedAt);

                TestContext.WriteLine(item.ClassName);
                TestContext.WriteLine(item.ObjectId);
                TestContext.WriteLine(item.CreatedAt);
                TestContext.WriteLine(item.UpdatedAt);
                TestContext.WriteLine(item["intValue"]);
                TestContext.WriteLine(item["boolValue"]);
                TestContext.WriteLine(item["stringValue"]);
            }
        }

        [Test]
        public async Task Count() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.WhereGreaterThan("balance", 200);
            int count = await query.Count();
            TestContext.WriteLine(count);
            Assert.Greater(count, 0);
        }

        [Test]
        public async Task OrderBy() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.OrderBy("balance");
            List<LCObject> results = await query.Find();
            Assert.LessOrEqual((int)results[0]["balance"], (int)results[1]["balance"]);

            query = new LCQuery<LCObject>("Account");
            query.OrderByDescending("balance");
            results = await query.Find();
            Assert.GreaterOrEqual((int)results[0]["balance"], (int)results[1]["balance"]);
        }

        [Test]
        public async Task Include() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
            query.Include("objectValue");
            LCObject hello = await query.Get("5e0d55aedd3c13006a53cd87");
            LCObject world = hello["objectValue"] as LCObject;
            TestContext.WriteLine(world["content"]);
            Assert.AreEqual(world["content"], "7788");
        }

        [Test]
        public async Task Get() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject account = await query.Get("5e0d9f7fd4b56c008e5d048a");
            Assert.AreEqual(account["balance"], 400);
        }

        [Test]
        public async Task First() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject account = await query.First();
            Assert.NotNull(account.ObjectId);
        }

        [Test]
        public async Task GreaterQuery() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.WhereGreaterThan("balance", 200);
            List<LCObject> list = await query.Find();
            TestContext.WriteLine(list.Count);
            Assert.Greater(list.Count, 0);
        }

        [Test]
        public async Task And() {
            LCQuery<LCObject> q1 = new LCQuery<LCObject>("Account");
            q1.WhereGreaterThan("balance", 100);
            LCQuery<LCObject> q2 = new LCQuery<LCObject>("Account");
            q2.WhereLessThan("balance", 500);
            LCQuery<LCObject> query = LCQuery<LCObject>.And(new List<LCQuery<LCObject>> { q1, q2 });
            List<LCObject> results = await query.Find();
            TestContext.WriteLine(results.Count);
            results.ForEach(item => {
                int balance = (int)item["balance"];
                Assert.IsTrue(balance >= 100 || balance <= 500);
            });
        }

        [Test]
        public async Task Or() {
            LCQuery<LCObject> q1 = new LCQuery<LCObject>("Account");
            q1.WhereLessThanOrEqualTo("balance", 100);
            LCQuery<LCObject> q2 = new LCQuery<LCObject>("Account");
            q2.WhereGreaterThanOrEqualTo("balance", 500);
            LCQuery<LCObject> query = LCQuery<LCObject>.Or(new List<LCQuery<LCObject>> { q1, q2 });
            List<LCObject> results = await query.Find();
            TestContext.WriteLine(results.Count);
            results.ForEach(item => {
                int balance = (int)item["balance"];
                Assert.IsTrue(balance <= 100 || balance >= 500);
            });
        }

        [Test]
        public async Task WhereObjectEquals() {
            LCQuery<LCObject> worldQuery = new LCQuery<LCObject>("World");
            LCObject world = await worldQuery.Get("5e0d55ae21460d006a1ec931");
            LCQuery<LCObject> helloQuery = new LCQuery<LCObject>("Hello");
            helloQuery.WhereEqualTo("objectValue", world);
            LCObject hello = await helloQuery.First();
            TestContext.WriteLine(hello.ObjectId);
            Assert.AreEqual(hello.ObjectId, "5e0d55aedd3c13006a53cd87");
        }

        [Test]
        public async Task Exist() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.WhereExists("user");
            List<LCObject> results = await query.Find();
            results.ForEach(item => {
                Assert.NotNull(item["user"]);
            });

            query = new LCQuery<LCObject>("Account");
            query.WhereDoesNotExist("user");
            results = await query.Find();
            results.ForEach(item => {
                Assert.IsNull(item["user"]);
            });
        }

        [Test]
        public async Task Select() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            query.Select("balance");
            List<LCObject> results = await query.Find();
            results.ForEach(item => {
                Assert.NotNull(item["balance"]);
                Assert.IsNull(item["user"]);
            });
        }

        [Test]
        public async Task String() {
            // Start
            LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
            query.WhereStartsWith("stringValue", "hello");
            List<LCObject> results = await query.Find();
            results.ForEach(item => {
                string str = item["stringValue"] as string;
                Assert.IsTrue(str.StartsWith("hello"));
            });

            // End
            query = new LCQuery<LCObject>("Hello");
            query.WhereEndsWith("stringValue", "world");
            results = await query.Find();
            results.ForEach(item => {
                string str = item["stringValue"] as string;
                Assert.IsTrue(str.EndsWith("world"));
            });

            // Contains
            query = new LCQuery<LCObject>("Hello");
            query.WhereContains("stringValue", ",");
            results = await query.Find();
            results.ForEach(item => {
                string str = item["stringValue"] as string;
                Assert.IsTrue(str.Contains(','));
            });
        }

        [Test]
        public async Task Array() {
            // equal
            LCQuery<LCObject> query = new LCQuery<LCObject>("Book");
            query.WhereEqualTo("pages", 3);
            List<LCObject>results = await query.Find();
            results.ForEach(item => {
                List<object> pages = item["pages"] as List<object>;
                Assert.IsTrue(pages.Contains(3));
            });

            // contain all
            List<int> containAlls = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
            query = new LCQuery<LCObject>("Book");
            query.WhereContainsAll("pages", containAlls);
            results = await query.Find();
            results.ForEach(item => {
                List<object> pages = item["pages"] as List<object>;
                pages.ForEach(i => {
                    Assert.IsTrue(pages.Contains(i));
                });
            });

            // contain in
            List<int> containIns = new List<int> { 4, 5, 6 };
            query = new LCQuery<LCObject>("Book");
            query.WhereContainedIn("pages", containIns);
            results = await query.Find();
            results.ForEach(item => {
                List<object> pages = item["pages"] as List<object>;
                bool f = false;
                containIns.ForEach(i => {
                    f |= pages.Contains(i);
                });
                Assert.IsTrue(f);
            });

            // size
            query = new LCQuery<LCObject>("Book");
            query.WhereSizeEqualTo("pages", 7);
            results = await query.Find();
            results.ForEach(item => {
                List<object> pages = item["pages"] as List<object>;
                Assert.AreEqual(pages.Count, 7);
            });
        }

        [Test]
        public async Task Geo() {
            LCObject obj = new LCObject("Todo");
            LCGeoPoint location = new LCGeoPoint(39.9, 116.4);
            obj["location"] = location;
            await obj.Save();

            // near
            LCQuery<LCObject> query = new LCQuery<LCObject>("Todo");
            LCGeoPoint point = new LCGeoPoint(39.91, 116.41);
            query.WhereNear("location", point);
            List<LCObject> results = await query.Find();
            Assert.Greater(results.Count, 0);

            // in box
            query = new LCQuery<LCObject>("Todo");
            LCGeoPoint southwest = new LCGeoPoint(30, 115);
            LCGeoPoint northeast = new LCGeoPoint(40, 118);
            query.WhereWithinGeoBox("location", southwest, northeast);
            results = await query.Find();
            Assert.Greater(results.Count, 0);
        }
    }
}
