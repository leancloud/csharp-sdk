using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class ACLTest : BaseTest {
        private Account account;

        [Test]
        [Order(0)]
        public async Task PrivateReadAndWrite() {
            Account account = new Account();
            account.ACL = new LCACL {
                PublicReadAccess = false,
                PublicWriteAccess = false
            };
            account.Balance = 1024;
            account["balance"] = 1024;
            await account.Save();
            Assert.IsFalse(account.ACL.PublicReadAccess);
            Assert.IsFalse(account.ACL.PublicWriteAccess);
        }

        [Test]
        [Order(1)]
        public async Task UserReadAndWrite() {
            await LCUser.Login(TestPhone, TestPhone);
            account = new Account();
            LCUser currentUser = await LCUser.GetCurrent();
            account.ACL = LCACL.CreateWithOwner(currentUser);
            account.Balance = 512;
            await account.Save();

            Assert.IsTrue(account.ACL.GetUserReadAccess(currentUser));
            Assert.IsTrue(account.ACL.GetUserWriteAccess(currentUser));

            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject result = await query.Get(account.ObjectId);
            TestContext.WriteLine(result.ObjectId);
            Assert.NotNull(result.ObjectId);

            await LCUser.Logout();

            try {
                await query.Get(account.ObjectId);
            } catch (LCException e) {
                Assert.AreEqual(e.Code, 403);
            }
        }

        [Test]
        [Order(2)]
        public async Task RoleReadAndWrite() {
            LCUser currentUser = await LCUser.Login(TestPhone, TestPhone);
            string name = $"role_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            LCACL roleACL = new LCACL();
            roleACL.SetUserReadAccess(currentUser, true);
            roleACL.SetUserWriteAccess(currentUser, true);
            LCRole role = LCRole.Create(name, roleACL);
            role.AddRelation("users", currentUser);
            await role.Save();

            account = new Account();
            LCACL acl = new LCACL();
            acl.SetRoleReadAccess(role, true);
            acl.SetRoleWriteAccess(role, true);
            account.ACL = acl;
            await account.Save();
            Assert.IsTrue(acl.GetRoleReadAccess(role));
            Assert.IsTrue(acl.GetRoleWriteAccess(role));
        }

        [Test]
        [Order(3)]
        public async Task Query() {
            await LCUser.Login(TestPhone, TestPhone);
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            Account queryAccount = (await query.Get(account.ObjectId)) as Account;
            TestContext.WriteLine(queryAccount.ObjectId);
            Assert.NotNull(queryAccount.ObjectId);
        }

        [Test]
        [Order(4)]
        public async Task Serialization() {
            await LCUser.Login("hello", "world");
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account") {
                IncludeACL = true
            };
            query.OrderByDescending("createdAt");
            ReadOnlyCollection<LCObject> accounts = await query.Find();
            foreach (LCObject account in accounts) {
                TestContext.WriteLine($"public read access: {account.ACL.PublicReadAccess}");
                TestContext.WriteLine($"public write access: {account.ACL.PublicWriteAccess}");
            }
        }
    }
}
