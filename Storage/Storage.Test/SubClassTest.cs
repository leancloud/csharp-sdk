using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    internal class Hello : LCObject {
        internal World World => this["objectValue"] as World;

        internal Hello() : base("Hello") { }
    }

    internal class World : LCObject {
        internal string Content {
            get {
                return this["content"] as string;
            } set {
                this["content"] = value;
            }
        }

        internal World() : base("World") { }
    }

    internal class Account : LCObject {
        internal int Balance {
            get {
                return (int)this["balance"];
            } set {
                this["balance"] = value;
            }
        }

        internal Account() : base("Account") { }
    }

    [TestFixture]
    public class SubClassTest {
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
        public async Task Create() {
            LCObject.RegisterSubclass<Account>("Account", () => new Account());
            Account account = new Account();
            account.Balance = 1000;
            await account.Save();
            TestContext.WriteLine(account.ObjectId);
            Assert.NotNull(account.ObjectId);
        }

        [Test]
        public async Task Query() {
            LCObject.RegisterSubclass<Account>("Account", () => new Account());
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
            LCObject.RegisterSubclass<Account>("Account", () => new Account());
            Account account = new Account() {
                Balance = 1024
            };
            await account.Save();
            await account.Delete();
        }

        [Test]
        public async Task Include() {
            LCObject.RegisterSubclass<Hello>("Hello", () => new Hello());
            LCObject.RegisterSubclass<World>("World", () => new World());

            LCQuery<Hello> helloQuery = new LCQuery<Hello>("Hello");
            helloQuery.Include("objectValue");
            Hello hello = await helloQuery.Get("5e0d55aedd3c13006a53cd87");
            World world = hello.World;

            TestContext.WriteLine(hello.ObjectId);
            Assert.AreEqual(hello.ObjectId, "5e0d55aedd3c13006a53cd87");
            TestContext.WriteLine(world.ObjectId);
            Assert.AreEqual(world.ObjectId, "5e0d55ae21460d006a1ec931");
            Assert.AreEqual(world.Content, "7788");
        }
    }
}
