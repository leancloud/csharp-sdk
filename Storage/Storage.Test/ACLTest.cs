using NUnit.Framework;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.Test {
    public class ACLTest {
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
            //await LCUser.Login('hello', 'world');
            //LCObject account = new LCObject('Account');
            //LCUser currentUser = await LCUser.getCurrent();
            //LCACL acl = LCACL.createWithOwner(currentUser);
            //account.acl = acl;
            //account['balance'] = 512;
            //await account.save();

            //assert(acl.getUserReadAccess(currentUser) == true);
            //assert(acl.getUserWriteAccess(currentUser) == true);

            //LCQuery<LCObject> query = new LCQuery('Account');
            //LCObject result = await query.get(account.objectId);
            //print(result.objectId);
            //assert(result.objectId != null);

            //LCUser.logout();
            //result = await query.get(account.objectId);
            //assert(result == null);
        }
    }
}
