using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class ACLTest {
        [SetUp]
        public void SetUp() {
            Utils.SetUp();
        }

        [TearDown]
        public void TearDown() {
            Utils.TearDown();
        }

        [Test]
        public async Task PrivateReadAndWrite() {
            LCObject account = new LCObject("Account");
            LCACL acl = new LCACL();
            acl.PublicReadAccess = false;
            acl.PublicWriteAccess = false;
            account.ACL = acl;
            account["balance"] = 1024;
            await account.Save();
            Assert.IsFalse(acl.PublicReadAccess);
            Assert.IsFalse(acl.PublicWriteAccess);
        }

        [Test]
        public async Task UserReadAndWrite() {
            await LCUser.Login("hello", "world");
            LCObject account = new LCObject("Account");
            LCUser currentUser = await LCUser.GetCurrent();
            LCACL acl = LCACL.CreateWithOwner(currentUser);
            account.ACL = acl;
            account["balance"] = 512;
            await account.Save();

            Assert.IsTrue(acl.GetUserReadAccess(currentUser));
            Assert.IsTrue(acl.GetUserWriteAccess(currentUser));

            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject result = await query.Get(account.ObjectId);
            TestContext.WriteLine(result.ObjectId);
            Assert.NotNull(result.ObjectId);

            await LCUser.Logout();
            result = await query.Get(account.ObjectId);
            Assert.IsNull(result);
        }

        [Test]
        public async Task RoleReadAndWrite() {
            LCQuery<LCRole> query = LCRole.GetQuery();
            LCRole owner = await query.Get("5e1440cbfc36ed006add1b8d");
            LCObject account = new LCObject("Account");
            LCACL acl = new LCACL();
            acl.SetRoleReadAccess(owner, true);
            acl.SetRoleWriteAccess(owner, true);
            account.ACL = acl;
            await account.Save();
            Assert.IsTrue(acl.GetRoleReadAccess(owner));
            Assert.IsTrue(acl.GetRoleWriteAccess(owner));
        }

        [Test]
        public async Task Query() {
            await LCUser.Login("game", "play");
            LCQuery<LCObject> query = new LCQuery<LCObject>("Account");
            LCObject account = await query.Get("5e144525dd3c13006a8f8de2");
            TestContext.WriteLine(account.ObjectId);
            Assert.NotNull(account.ObjectId);
        }

        [Test]
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
