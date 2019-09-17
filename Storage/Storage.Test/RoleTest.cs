using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeanCloud;

namespace LeanCloud.Test {
    [TestFixture]
    public class RoleTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina(true);
        }

        [Test]
        public async Task GetUsersFromRole() {
            AVQuery<AVRole> query = new AVQuery<AVRole>();
            AVRole role = await query.FirstAsync();
            AVQuery<AVUser> userQuery = role.Users.Query;
            IEnumerable<AVUser> users = await userQuery.FindAsync();
            Assert.Greater(users.Count(), 0);
            TestContext.Out.WriteLine($"count: {users.Count()}");
            foreach (AVUser user in users) {
                TestContext.Out.WriteLine($"{user.ObjectId}, {user.Username}");
            }
        }
    }
}
