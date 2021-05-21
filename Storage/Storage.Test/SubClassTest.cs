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

        [Test]
        public async Task ObjectWithFile() {
            LCUser user = await LCUser.Login("hello", "world");
            ObjectWithFile obj = new ObjectWithFile() {
                File = new LCFile("avatar", "../../../../../assets/hello.png"),
                Owner = user
            };
            await obj.Save();

            LCQuery<ObjectWithFile> query = new LCQuery<ObjectWithFile>("ObjectWithFile");
            ObjectWithFile obj2 = await query.Get(obj.ObjectId);

            TestContext.WriteLine(obj2.File.Url);
            TestContext.WriteLine(obj2.Owner.ObjectId);

            Assert.IsNotNull(obj2.File.Url);
            Assert.IsNotNull(obj2.Owner.ObjectId);
        }
    }
}
