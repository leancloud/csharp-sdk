using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud;

namespace LeanCloudTests {
    public class ObjectControllerTests {
        [SetUp]
        public void SetUp() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz",
                ApplicationKey = "pbf6Nk5seyjilexdpyrPwjSp",
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
        }

        [Test]
        public async Task TestSave() {
            TestContext.Out.WriteLine($"before at {Thread.CurrentThread.ManagedThreadId}");
            var obj = AVObject.Create("Foo");
            obj["content"] = "hello, world";
            await obj.SaveAsync();
            TestContext.Out.WriteLine($"saved at {Thread.CurrentThread.ManagedThreadId}");
            Assert.NotNull(obj.ObjectId);
            Assert.NotNull(obj.CreatedAt);
            Assert.NotNull(obj.UpdatedAt);
        } 
    }
}
