using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage;
using LeanCloud.Common;

namespace LeanCloud.Test {
    public class OperationTest {
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
        public async Task Increment() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject account = await query.Get("5e154a5143c257006fbff63f");
            TestContext.WriteLine(account["balance"]);
            int balance = (int)account["balance"];
            account.Increment("balance", 100);
            await account.Save();
            TestContext.WriteLine(account["balance"]);
            Assert.AreEqual((int)account["balance"], balance + 100);
        }

        [Test]
        public async Task Decrement() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject account = await query.Get("5e154a5143c257006fbff63f");
            TestContext.WriteLine(account["balance"]);
            int balance = (int)account["balance"];
            account.Increment("balance", -10);
            await account.Save();
            TestContext.WriteLine(account["balance"]);
            Assert.AreEqual((int)account["balance"], balance - 10);
        }

        [Test]
        public async Task AddAndRemove() {
            LCObject book = new LCObject("Book");
            book["pages"] = new List<int> { 1, 2, 3, 4, 5 };
            await book.Save();

            // add
            book.Add("pages", 6);
            await book.Save();
            TestContext.WriteLine(book["pages"]);
            Assert.AreEqual((book["pages"] as List<object>).Count, 6);
            book.AddAll("pages", new List<int> { 7, 8, 9 });
            await book.Save();
            TestContext.WriteLine(book["pages"]);
            Assert.AreEqual((book["pages"] as List<object>).Count, 9);

            // remove
            book.Remove("pages", 2);
            TestContext.WriteLine(book["pages"]);
            await book.Save();
            Assert.AreEqual((book["pages"] as List<object>).Count, 8);
            book.RemoveAll("pages", new List<int> { 1, 2, 3 });
            await book.Save();
            TestContext.WriteLine(book["pages"]);
            Assert.AreEqual((book["pages"] as List<object>).Count, 6);
        }

        [Test]
        public async Task AddUnique() {
            LCObject book = new LCObject("Book");
            book["pages"] = new List<int> { 1, 2, 3, 4, 5 };
            await book.Save();

            // add
            book.AddUnique("pages", 1);
            await book.Save();
            TestContext.WriteLine(book["pages"]);
            Assert.AreEqual((book["pages"] as List<object>).Count, 5);

            book.AddAllUnique("pages", new List<int> { 5, 6, 7 });
            await book.Save();
            TestContext.WriteLine(book["pages"]);
            Assert.AreEqual((book["pages"] as List<object>).Count, 7);
        }
    }
}
