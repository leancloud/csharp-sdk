using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.Test {
    public class HelloTest {
        [SetUp]
        public void SetUp() {
            LeanCloud.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
            Logger.LogDelegate += Utils.Print;
        }

        [Test]
        public async Task Run() {
            Dictionary<string, object> parameters = new Dictionary<string, object> {
                { "name", "world" }
            };
            Dictionary<string, object> response = await LCCloud.Run("hello", parameters);
            string ret = response["result"] as string;
            TestContext.WriteLine($"ret: {ret}");
            Assert.AreEqual(ret, "hello, world");
        }
    }
}
