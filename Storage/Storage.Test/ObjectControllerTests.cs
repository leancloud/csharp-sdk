using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;

namespace LeanCloud.Test {
    public class ObjectControllerTests {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task Save() {
            AVObject obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            obj["list"] = new List<int> { 1, 1, 2, 3, 5, 8 };
            obj["dict"] = new Dictionary<string, int> {
                { "hello", 1 },
                { "world", 2 }
            };
            await obj.SaveAsync();
            Assert.NotNull(obj.ObjectId);
            Assert.NotNull(obj.CreatedAt);
            Assert.NotNull(obj.UpdatedAt);
        }

        [Test]
        public async Task SaveWithOptions() {
            AVObject account = AVObject.CreateWithoutData("Account", "5d65fa5330863b008065e476");
            account["balance"] = 100;
            await account.SaveAsync();
            AVQuery<AVObject> query = new AVQuery<AVObject>("Account");
            query.WhereGreaterThan("balance", 80);
            account["balance"] = 50;
            await account.SaveAsync(true, query);
            TestContext.Out.WriteLine($"balance: {account["balance"]}");
        }

        [Test]
        public async Task SaveWithPointer() {
            AVObject comment = new AVObject("Comment") {
                { "content", "Hello, Comment" }
            };
            
            AVObject post = new AVObject("Post") {
                { "name", "New Post" },
                { "category", new AVObject("Category") }
            };
            comment["post"] = post;

            AVObject testPost = new AVObject("Post") {
                { "name", "Test Post" }
            };
            comment["test_post"] = testPost;

            await comment.SaveAsync();
        }

        [Test]
        public async Task SaveBatch() {
            List<AVObject> objList = new List<AVObject>();
            for (int i = 0; i < 5; i++) {
                AVObject obj = AVObject.Create("Foo");
                obj["content"] = "batch object";
                objList.Add(obj);
            }
            await objList.SaveAllAsync();
            objList.ForEach(obj => {
                Assert.NotNull(obj.ObjectId);
            });
        }

        [Test]
        public async Task Fetch() {
            AVObject obj = AVObject.CreateWithoutData("Todo", "5d5f6039d5de2b006cf29c8f");
            await obj.FetchAsync();
            Assert.NotNull(obj["title"]);
            Assert.NotNull(obj["content"]);
            TestContext.Out.WriteLine($"{obj["title"]}, {obj["content"]}");
        }

        [Test]
        public async Task FetchWithKeys() {
            AVObject obj = AVObject.CreateWithoutData("Post", "5d3abfa530863b0068e1b326");
            await obj.FetchAsync(new List<string> { "pubUser" });
            TestContext.Out.WriteLine($"{obj["pubUser"]}");
        }

        [Test]
        public async Task FetchWithIncludes() {
            AVObject obj = AVObject.CreateWithoutData("Post", "5d3abfa530863b0068e1b326");
            await obj.FetchAsync(includes: new List<string> { "tag" });
            AVObject tag = obj["tag"] as AVObject;
            TestContext.Out.WriteLine($"{tag["name"]}");
        }

        [Test]
        public async Task FetchAll() {
            List<AVObject> objList = new List<AVObject> {
                AVObject.CreateWithoutData("Tag", "5d64e5ebc05a8000730340ba"),
                AVObject.CreateWithoutData("Tag", "5d64e5eb12215f0073db271c"),
                AVObject.CreateWithoutData("Tag", "5d64e57f43e78c0068a14315")
            };
            await objList.FetchAllAsync();
            objList.ForEach(obj => {
                Assert.NotNull(obj.ObjectId);
                TestContext.Out.WriteLine($"{obj.ObjectId}, {obj["name"]}");
            });
        }

        [Test]
        public async Task Delete() {
            AVObject obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            await obj.SaveAsync();
            Assert.NotNull(obj);
            await obj.DeleteAsync();
        }

        [Test]
        public async Task DeleteAll() {
            List<AVObject> objList = new List<AVObject>();
            for (int i = 0; i < 5; i++) {
                AVObject obj = AVObject.Create("Foo");
                obj["content"] = "batch object";
                objList.Add(obj);
            }
            await objList.SaveAllAsync();
            await AVObject.DeleteAllAsync(objList);
        }

        [Test]
        public void Set() {
            AVObject obj = AVObject.Create("Foo");
            obj["hello"] = "world";
            TestContext.Out.WriteAsync(obj["hello"] as string);
        }
    }
}
