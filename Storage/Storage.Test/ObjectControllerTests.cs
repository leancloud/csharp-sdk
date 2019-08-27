using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud;

namespace LeanCloudTests {
    public class ObjectControllerTests {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task TestSave() {
            TestContext.Out.WriteLine($"before at {Thread.CurrentThread.ManagedThreadId}");
            var obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            await obj.SaveAsync();
            TestContext.Out.WriteLine($"{obj.ObjectId} saved at {Thread.CurrentThread.ManagedThreadId}");
            Assert.NotNull(obj.ObjectId);
            Assert.NotNull(obj.CreatedAt);
            Assert.NotNull(obj.UpdatedAt);
        }

        [Test]
        public async Task ObjectFetch() {
            AVObject obj = AVObject.CreateWithoutData("Todo", "5d5f6039d5de2b006cf29c8f");
            await obj.FetchAsync();
            Assert.NotNull(obj["title"]);
            Assert.NotNull(obj["content"]);
            TestContext.Out.WriteLine($"{obj["title"]}, {obj["content"]}");
        }
    }
}
