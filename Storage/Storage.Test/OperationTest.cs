using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace Storage.Test {
    public class OperationTest : BaseTest {
        [Test]
        public async Task Increment() {
            Account account = new Account {
                Balance = 10
            };
            await account.Save();
            TestContext.WriteLine(account.Balance);
            account.Increment("balance", 100);
            await account.Save();
            TestContext.WriteLine(account.Balance);
            Assert.AreEqual(account.Balance, 110);
        }

        [Test]
        public async Task Decrement() {
            Account account = new Account {
                Balance = 100
            };
            await account.Save();
            TestContext.WriteLine(account.Balance);
            account.Increment("balance", -10);
            await account.Save();
            TestContext.WriteLine(account.Balance);
            Assert.AreEqual(account.Balance, 90);
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
