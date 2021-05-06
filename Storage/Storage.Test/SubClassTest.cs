using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using LeanCloud.Storage;

namespace Storage.Test {
    public class SubClassTest : BaseTest {
        [Test]
        public async Task Create() {
            Account account = new Account();
            account.Balance = 1000;
            await account.Save();
            TestContext.WriteLine(account.ObjectId);
            Assert.NotNull(account.ObjectId);
        }

        [Test]
        public async Task Query() {
            LCQuery<Account> query = new LCQuery<Account>("Account");
            query.WhereGreaterThan("balance", 500);
            ReadOnlyCollection<Account> list = await query.Find();
            TestContext.WriteLine(list.Count);
            Assert.Greater(list.Count, 0);
            foreach (Account account in list) {
                Assert.NotNull(account.ObjectId);
            }
        }

        [Test]
        public async Task Delete() {
            Account account = new Account() {
                Balance = 1024
            };
            await account.Save();
            await account.Delete();
        }
    }
}
