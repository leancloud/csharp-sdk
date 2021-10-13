using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud;
using LeanCloud.Storage;

namespace Storage.Test {
    public class CloudTest : BaseTest {
        [Test]
        public async Task Ping() {
            Dictionary<string, object> response = await LCCloud.Run("ping");
            TestContext.WriteLine(response["result"]);
            Assert.AreEqual(response["result"], "pong");
        }

        [Test]
        public async Task Hello() {
            string result = await LCCloud.Run<string>("hello", new Dictionary<string, object> {
                { "name", "world" }
            });
            TestContext.WriteLine(result);
            Assert.AreEqual(result, "hello, world");
        }

        [Test]
        public async Task GetObject() {
            LCObject hello = new LCObject("Hello");
            await hello.Save();
            object reponse = await LCCloud.RPC("getObject", new Dictionary<string, object> {
                { "className", "Hello" },
                { "id", hello.ObjectId }
            });
            LCObject obj = reponse as LCObject;
            Assert.AreEqual(obj.ObjectId, hello.ObjectId);
        }

        [Test]
        public async Task GetObjects() {
            object response = await LCCloud.RPC("getObjects");
            List<object> list = response as List<object>;
            IEnumerable<LCObject> objects = list.Cast<LCObject>();
            TestContext.WriteLine(objects.Count());
            Assert.Greater(objects.Count(), 0);
            foreach (LCObject obj in objects) {
                int balance = (int)obj["balance"];
                Assert.Greater(balance, 100);
            }
        }

        [Test]
        public async Task GetObjectMap() {
            object response = await LCCloud.RPC("getObjectMap");
            Dictionary<string, object> dict = response as Dictionary<string, object>;
            TestContext.WriteLine(dict.Count);
            Assert.Greater(dict.Count, 0);
            foreach (KeyValuePair<string, object> kv in dict) {
                LCObject obj = kv.Value as LCObject;
                Assert.AreEqual(kv.Key, obj.ObjectId);
            }
        }

        [Test]
        public void CatchLCException() {
            LCException ex = Assert.CatchAsync<LCException>(() => LCCloud.Run("lcexception"));
            Assert.AreEqual(ex.Code, 123);
            Assert.AreEqual(ex.Message, "Runtime exception");
        }

        [Test]
        public void CatchException() {
            LCException ex = Assert.CatchAsync<LCException>(() => LCCloud.Run("exception"));
            Assert.AreEqual(ex.Code, 1);
            Assert.AreEqual(ex.Message, "Hello, exception");
        }
    }
}
