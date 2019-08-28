using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LeanCloud;

namespace LeanCloudTests {
    public class ObjectControllerTests {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task Save() {
            TestContext.Out.WriteLine($"before at {Thread.CurrentThread.ManagedThreadId}");
            AVObject obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            await obj.SaveAsync();
            TestContext.Out.WriteLine($"{obj.ObjectId} saved at {Thread.CurrentThread.ManagedThreadId}");
            Assert.NotNull(obj.ObjectId);
            Assert.NotNull(obj.CreatedAt);
            Assert.NotNull(obj.UpdatedAt);
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
    }
}
