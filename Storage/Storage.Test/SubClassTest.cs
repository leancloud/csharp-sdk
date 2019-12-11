using NUnit.Framework;
using LeanCloud;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LeanCloud.Test {
    [AVClassName("Account")]
    public class Account : AVObject {
        [AVFieldName("name")]
        public string Name {
            get {
                return GetProperty<string>("Name");
            }
            set {
                SetProperty(value, "Name");
            }
        }

        [AVFieldName("balance")]
        public int Balance {
            get {
                return GetProperty<int>("Balance");
            }
            set {
                SetProperty(value, "Balance");
            }
        }
    }

    [TestFixture]
    public class SubClassTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina(true);
        }

        [Test]
        public async Task SubClass() {
            AVObject.RegisterSubclass<Account>();
            AVQuery<Account> query = new AVQuery<Account>();
            IEnumerable<Account> accounts = await query.FindAsync();
            foreach (Account account in accounts) {
                Assert.NotNull(account.Name);
                Assert.Greater(account.Balance, 0);
                TestContext.Out.WriteLine($"{account.Name}, {account.Balance}");
            }
        }
    }
}
