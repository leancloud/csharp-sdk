using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using LeanCloud;
using LeanCloud.Storage;

using static NUnit.Framework.TestContext;

namespace Storage.Test {
    public class ObjectTest : BaseTest {
        [Test]
        public async Task CreateObject() {
            Hello @object = new Hello();
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

            WriteLine(@object.ClassName);
            WriteLine(@object.ObjectId);
            WriteLine(@object.CreatedAt);
            WriteLine(@object.UpdatedAt);
            WriteLine(@object["intValue"]);
            WriteLine(@object["boolValue"]);
            WriteLine(@object["stringValue"]);
            WriteLine(@object["objectValue"]);
            WriteLine(@object["time"]);

            Assert.AreEqual(nestedObj, @object["objectValue"]);
            WriteLine(nestedObj.ClassName);
            WriteLine(nestedObj.ObjectId);

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
                World world = new World {
                    Content = $"word_{i}"
                };
                list.Add(world);
            }
            await LCObject.SaveAll(list);
            foreach (LCObject obj in list) {
                Assert.NotNull(obj.ObjectId);
            }
        }

        [Test]
        public async Task Delete() {
            World world = new World();
            await world.Save();
            await world.Delete();
        }

        [Test]
        public async Task DeleteAll() {
            List<World> list = new List<World> {
                new World(),
                new World(),
                new World(),
                new World(),
            };
            await LCObject.SaveAll(list);
            await LCObject.DeleteAll(list);
        }

        [Test]
        public async Task Fetch() {
            Hello hello = new Hello {
                World = new World {
                    Content = "7788"
                }
            };
            await hello.Save();

            hello = LCObject.CreateWithoutData("Hello", hello.ObjectId) as Hello;
            await hello.Fetch(includes: new string[] { "objectValue" });
            World world = hello.World;
            WriteLine(world.Content);
            Assert.AreEqual(world.Content, "7788");
        }

        [Test]
        public async Task SaveWithOption() {
            Account account = new Account {
                Balance = 10
            };
            await account.Save();

            LCQuery<LCObject> q = new LCQuery<LCObject>("Account");
            q.WhereGreaterThan("balance", 100);
            try {
                await account.Save(fetchWhenSave: true, query: q);
            } catch(LCException e) {
                WriteLine($"{e.Code} : {e.Message}");
                Assert.AreEqual(e.Code, 305);
            }
        }

        [Test]
        public async Task Unset() {
            Hello hello = new Hello {
                World = new World()
            };
            await hello.Save();
            Assert.NotNull(hello.World);

            hello.Unset("objectValue");
            await hello.Save();
            Assert.IsNull(hello.World);
        }

        [Test]
        public async Task OperateNullProperty() {
            Hello obj = new Hello();
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
            LCQuery<LCObject> query = new LCQuery<LCObject>("Hello");
            IEnumerable<string> ids = (await query.Find()).Select(obj => obj.ObjectId);
            IEnumerable<LCObject> list = ids.Select(id => LCObject.CreateWithoutData("Hello", id));
            await LCObject.FetchAll(list);
            Assert.Greater(list.Count(), 0);
            foreach (LCObject obj in list) {
                Assert.NotNull(obj.CreatedAt);
            }
        }

        [Test]
        public async Task Serialization() {
            Hello obj = new Hello();
            obj["intValue"] = 123;
            obj["boolValue"] = true;
            obj["stringValue"] = "hello, world";
            obj["time"] = DateTime.Now;
            obj["intList"] = new List<int> { 1, 1, 2, 3, 5, 8 };
            obj["stringMap"] = new Dictionary<string, object> {
                { "k1", 111 },
                { "k2", true },
                { "k3", "haha" }
            };
            LCObject nestedObj = new LCObject("World");
            nestedObj["content"] = "7788";
            obj["objectValue"] = nestedObj;
            obj["pointerList"] = new List<object> {
                new LCObject("World"),
                nestedObj
            };
            await obj.Save();

            string json = obj.ToString();
            WriteLine(json);
            LCObject newObj = LCObject.ParseObject(json);
            Assert.NotNull(newObj.ObjectId);
            Assert.NotNull(newObj.ClassName);
            Assert.NotNull(newObj.CreatedAt);
            Assert.NotNull(newObj.UpdatedAt);
            Assert.AreEqual(newObj["intValue"], 123);
            Assert.AreEqual(newObj["boolValue"], true);
            Assert.AreEqual(newObj["stringValue"], "hello, world");
        }
    }
}
