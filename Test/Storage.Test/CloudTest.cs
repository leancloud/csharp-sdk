using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Storage;

namespace LeanCloud.Test {
    public class CloudTest {
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
        public async Task Call() {
            Dictionary<string, object> response = await LCCloud.Run("hello", parameters: new Dictionary<string, object> {
                { "name", "world" }
            });
            TestContext.WriteLine(response["result"]);
            Assert.AreEqual(response["result"], "hello, world");
        }

        [Test]
        public async Task CallWithoutParams() {
            await LCCloud.Run("hello");
        }

        [Test]
        public async Task RPC() {
            List<object> result = await LCCloud.RPC("getTycoonList") as List<object>;
            IEnumerable<LCObject> tycoonList = result.Cast<LCObject>();
            foreach (LCObject item in tycoonList) {
                TestContext.WriteLine(item.ObjectId);
                Assert.NotNull(item.ObjectId);
            }
        }
    }
}
