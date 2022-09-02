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
            @object.FloatValue = 3.14f;
            @object.DoubleValue = 3.1415926;
            @object["boolValue"] = true;
            @object["stringValue"] = "hello, world";
            DateTime now = DateTime.Parse("2022-04-22T05:05:21.0005369Z");
            @object["time"] = now;
            @object["intList"] = new List<int> { 1, 1, 2, 3, 5, 8 };
            @object["stringMap"] = new Dictionary<string, object> {
                { "k1", 111 },
                { "k2", true },
                { "k3", "haha" }
            };
            World nestedObj = new World();
            nestedObj.Content = "7788";
            @object.World = nestedObj;
            @object["pointerList"] = new List<object> { new LCObject("World"), nestedObj };
            await @object.Save();

            WriteLine(@object.ClassName);
            WriteLine(@object.ObjectId);
            WriteLine(@object.CreatedAt);
            WriteLine(@object.UpdatedAt);
            WriteLine(@object["intValue"]);
            WriteLine(@object.FloatValue);
            WriteLine(@object.DoubleValue);
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
            Assert.AreEqual(@object.FloatValue, 3.14f);
            Assert.AreEqual(@object.DoubleValue, 3.1415926);
            Assert.AreEqual(@object["boolValue"], true);
            Assert.AreEqual(@object["stringValue"], "hello, world");
            Assert.AreEqual(@object["time"], now);

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

            foreach (LCObject obj in list) {
                World world = obj as World;
                world.Content = $"world";
            }
            await LCObject.SaveAll(list);
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

            World world = new World {
                Content = "hello, world"
            };
            obj["pointerList"] = new List<object> {
                world,
                new LCObject("World")
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
            Assert.AreEqual((newObj["objectValue"] as LCObject)["content"], "7788");

            Assert.IsTrue((newObj["pointerList"] as List<object>)[0] is World);
            World newWorld = (newObj["pointerList"] as List<object>)[0] as World;
            Assert.AreEqual(newWorld.Content, "hello, world");
        }

        [Test]
        public async Task FetchWhenSave() {
            LCObject hello = LCObject.Create("Hello");
            hello["intValue"] = 0;
            await hello.Save();

            string objectId = hello.ObjectId;

            LCObject incrHello = LCObject.CreateWithoutData("Hello", objectId);
            incrHello.Increment("intValue", 1);
            await incrHello.Save(true);
            WriteLine($"intValue: {incrHello["intValue"]}");
            Assert.AreEqual(incrHello["intValue"], 1);

            LCObject incrHello2 = LCObject.CreateWithoutData("Hello", objectId);
            incrHello2.Increment("intValue", 1);
            await incrHello2.Save(true);
            WriteLine($"intValue2: {incrHello2["intValue"]}");
            Assert.AreEqual(incrHello2["intValue"], 2);
        }
    }
}
