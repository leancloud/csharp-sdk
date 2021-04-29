using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace Storage.Test {
    public class RoleTest : BaseTest {
        [Test]
        public async Task NewRole() {
            LCUser currentUser = await LCUser.Login(TestPhone, TestPhone);
            LCACL acl = new LCACL {
                PublicReadAccess = true
            };
            acl.SetUserWriteAccess(currentUser, true);
            string name = $"role_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            LCRole role = LCRole.Create(name, acl);
            role.AddRelation("users", currentUser);
            await role.Save();
        }

        [Test]
        public async Task Query() {
            LCQuery<LCRole> query = LCRole.GetQuery();
            ReadOnlyCollection<LCRole> results = await query.Find();
            foreach (LCRole item in results) {
                TestContext.WriteLine($"{item.ObjectId} : {item.Name}");
                Assert.NotNull(item.ObjectId);
                Assert.NotNull(item.Name);
                TestContext.WriteLine(item.Roles.GetType());
                TestContext.WriteLine(item.Users.GetType());
                Assert.IsTrue(item.Roles is LCRelation<LCRole>);
                Assert.IsTrue(item.Users is LCRelation<LCUser>);
            }
        }
    }
}
