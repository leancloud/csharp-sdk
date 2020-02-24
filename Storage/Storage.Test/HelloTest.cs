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

        [Test]
        public async Task Query() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
            query.Limit(30);
            List<LCObject> results = await query.Find();
            TestContext.WriteLine(results.Count);
            foreach (LCObject obj in results) {
                TestContext.WriteLine(obj.ObjectId);
                Assert.NotNull(obj.ObjectId);
            }
        }

        [Test]
        public void InitByNull() {
            List<string> sl = new List<string> { "a", "a", "b" };
            HashSet<string> ss = new HashSet<string>(sl);
            TestContext.WriteLine(ss.Count);
        }

        [Test]
        public async Task Save() {
            LCObject hello = new LCObject("Hello");
            await hello.Save();
            TestContext.WriteLine($"object id: {hello.ObjectId}");
            Assert.NotNull(hello.ObjectId);
        }
    }
}
