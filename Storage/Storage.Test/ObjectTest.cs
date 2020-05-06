using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;
using LeanCloud.Storage;

using static NUnit.Framework.TestContext;

namespace Storage.Test {
    public class ObjectTest {
        [SetUp]
        public void SetUp() {
            LCLogger.LogDelegate += Utils.Print;
            LCApplication.Initialize("ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz", "NUKmuRbdAhg1vrb2wexYo1jo", "https://ikggdre2.lc-cn-n1-shared.com");
        }

        [TearDown]
        public void TearDown() {
            LCLogger.LogDelegate -= Utils.Print;
        }

        [Test]
        public async Task CreateObject() {
            LCObject @object = new LCObject("Hello");
            @object["intValue"] = 123;
            @object["boolValue"] = true;
            @object["stringValue"] = "hello, world";
            @object["time"] = DateTime.Now;
            @object["intList"] = new List<int> { 1, 1, 2, 3, 5, 8 };
            @object["stringMap"] = new Dictionary<string, object> {
                { "k1", 111 },
                { "k2", true },
                { "k3", "haha" }
            };
            LCObject nestedObj = new LCObject("World");
            nestedObj["content"] = "7788";
            @object["objectValue"] = nestedObj;
            @object["pointerList"] = new List<object> { new LCObject("World"), nestedObj };
            await @object.Save();

            TestContext.WriteLine(@object.ClassName);
            TestContext.WriteLine(@object.ObjectId);
            TestContext.WriteLine(@object.CreatedAt);
            TestContext.WriteLine(@object.UpdatedAt);
            TestContext.WriteLine(@object["intValue"]);
            TestContext.WriteLine(@object["boolValue"]);
            TestContext.WriteLine(@object["stringValue"]);
            TestContext.WriteLine(@object["objectValue"]);
            TestContext.WriteLine(@object["time"]);

            Assert.AreEqual(nestedObj, @object["objectValue"]);
            TestContext.WriteLine(nestedObj.ClassName);
            TestContext.WriteLine(nestedObj.ObjectId);

            Assert.NotNull(@object.ObjectId);
            Assert.NotNull(@object.ClassName);
            Assert.NotNull(@object.CreatedAt);
            Assert.NotNull(@object.UpdatedAt);
            Assert.AreEqual(@object["intValue"], 123);
            Assert.AreEqual(@object["boolValue"], true);
            Assert.AreEqual(@object["stringValue"], "hello, world");

            Assert.NotNull(nestedObj);
            Assert.NotNull(nestedObj.ClassName);
            Assert.NotNull(nestedObj.ObjectId);
            Assert.NotNull(nestedObj.CreatedAt);
            Assert.NotNull(nestedObj.UpdatedAt);

            List<object> pointerList = @object["pointerList"] as List<object>;
            foreach (object pointerObj in pointerList) {
                LCObject pointer = pointerObj as LCObject;
                Assert.NotNull(pointer.ObjectId);
            }
        }

        [Test]
        public async Task SaveAll() {
            List<LCObject> list = new List<LCObject>();
            for (int i = 0; i < 5; i++) {
                LCObject world = new LCObject("World");
                world["content"] = $"word_{i}";
                list.Add(world);
            }
            await LCObject.SaveAll(list);
            foreach (LCObject obj in list) {
                Assert.NotNull(obj.ObjectId);
            }
        }

        [Test]
        public async Task Delete() {
            LCObject world = new LCObject("World");
            await world.Save();
            await world.Delete();
        }

        [Test]
        public async Task DeleteAll() {
            List<LCObject> list = new List<LCObject> {
                new LCObject("World"),
                new LCObject("World"),
                new LCObject("World"),
                new LCObject("World")
            };
            await LCObject.SaveAll(list);
            await LCObject.DeleteAll(list);
        }

        [Test]
        public async Task Fetch() {
            LCObject hello = LCObject.CreateWithoutData("Hello", "5e14392743c257006fb769d5");
            await hello.Fetch(includes: new List<string> { "objectValue" });
            LCObject world = hello["objectValue"] as LCObject;
            TestContext.WriteLine(world["content"]);
            Assert.AreEqual(world["content"], "7788");
        }

        [Test]
        public async Task SaveWithOption() {
            LCObject account = new LCObject("Account");
            account["balance"] = 10;
            await account.Save();

            account["balance"] = 1000;
            LCQuery<LCObject> q = new LCQuery<LCObject>("Account");
            q.WhereGreaterThan("balance", 100);
            try {
                await account.Save(fetchWhenSave: true, query: q);
            } catch(LCException e) {
                TestContext.WriteLine($"{e.Code} : {e.Message}");
                Assert.AreEqual(e.Code, 305);
            }
        }

        [Test]
        public async Task Unset() {
            LCObject hello = new LCObject("Hello");
            hello["content"] = "hello, world";
            await hello.Save();
            TestContext.WriteLine(hello["content"]);
            Assert.AreEqual(hello["content"], "hello, world");

            hello.Unset("content");
            await hello.Save();
            TestContext.WriteLine(hello["content"]);
            Assert.IsNull(hello["content"]);
        }

        [Test]
        public async Task OperateNullProperty() {
            LCObject obj = new LCObject("Hello");
            obj.Increment("intValue", 123);
            obj.Increment("intValue", 321);
            obj.Add("intList", 1);
            obj.Add("intList", 2);
            obj.Add("intList", 3);
            await obj.Save();

            WriteLine(obj["intValue"]);
            Assert.AreEqual(obj["intValue"], 444);
            List<object> intList = obj["intList"] as List<object>;
            WriteLine(intList.Count);
            Assert.AreEqual(intList.Count, 3);
            Assert.AreEqual(intList[0], 1);
            Assert.AreEqual(intList[1], 2);
            Assert.AreEqual(intList[2], 3);
        }

        [Test]
        public async Task FetchAll() {
            List<LCObject> list = new List<LCObject> {
                LCObject.CreateWithoutData("Hello", "5e8fe86938ed12000870ae82"),
                LCObject.CreateWithoutData("Hello", "5e8fe867158a7a0006be0feb"),
                LCObject.CreateWithoutData("Hello", "5e8fe84e5c385800081a1d64"),
            };
            await LCObject.FetchAll(list);
            Assert.Greater(list.Count, 0);
            foreach (LCObject obj in list) {
                Assert.NotNull(obj["intList"]);
            }
        }
    }
}
