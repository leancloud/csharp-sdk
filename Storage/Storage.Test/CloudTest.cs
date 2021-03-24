using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class CloudTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            //LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
            LCApplication.Initialize("8ijVI3gBAnPGynW0rVfh5gHP-gzGzoHsz", "265r8JSHhNYpV0qIJBvUWrQY", "https://8ijvi3gb.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task Call() {
            Dictionary<string, object> response = await LCCloud.Run("hello", parameters: new Dictionary<string, object> {
                { "name", "world" }
            });
            TestContext.WriteLine(response["result"]);
            Assert.AreEqual(response["result"], "Hello, world!");
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

        [Test]
        public async Task RPCObject() {
            LCQuery<LCObject> query = new LCQuery<LCObject>("Todo");
            LCObject todo = await query.Get("6052cd87b725a143ea83dbf8");
            object result = await LCCloud.RPC("getTodo", todo);
            LCObject obj = result as LCObject;
            TestContext.WriteLine(obj.ToString());
        }

        [Test]
        public async Task RPCObjects() {
            Dictionary<string, object> parameters = new Dictionary<string, object> {
                { "limit", 20 }
            };
            List<object> result = await LCCloud.RPC("getTodos", parameters) as List<object>;
            IEnumerable<LCObject> todos = result.Cast<LCObject>();
            foreach (LCObject todo in todos) {
                TestContext.WriteLine(todo.ObjectId);
            }
        }

        [Test]
        public async Task RPCObjectMap() {
            Dictionary<string, object> result = await LCCloud.RPC("getTodoMap") as Dictionary<string, object>;
            foreach (KeyValuePair<string, object> kv in result) {
                LCObject todo = kv.Value as LCObject;
                TestContext.WriteLine(todo.ObjectId);
            }
        }
    }
}
