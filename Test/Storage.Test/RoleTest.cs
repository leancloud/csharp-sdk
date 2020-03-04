using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Storage;
using LeanCloud.Common;

namespace LeanCloud.Test {
    [TestFixture]
    public class RoleTest {
        [SetUp]
        public void SetUp() {
            Logger.LogDelegate += Utils.Print;
            LeanCloud.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            Logger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task NewRole() {
            LCUser currentUser = await LCUser.Login("game", "play");
            LCACL acl = new LCACL();
            acl.PublicReadAccess = true;
            acl.SetUserWriteAccess(currentUser, true);
            string name = $"role_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            LCRole role = LCRole.Create(name, acl);
            role.AddRelation("users", currentUser);
            await role.Save();
        }

        [Test]
        public async Task Query() {
            LCQuery<LCRole> query = LCRole.GetQuery();
            List<LCRole> list = await query.Find();
            list.ForEach(item => {
                TestContext.WriteLine($"{item.ObjectId} : {item.Name}");
                Assert.NotNull(item.ObjectId);
                Assert.NotNull(item.Name);
                TestContext.WriteLine(item.Roles.GetType());
                TestContext.WriteLine(item.Users.GetType());
                Assert.IsTrue(item.Roles is LCRelation<LCRole>);
                Assert.IsTrue(item.Users is LCRelation<LCUser>);
            });
        }
    }
}
