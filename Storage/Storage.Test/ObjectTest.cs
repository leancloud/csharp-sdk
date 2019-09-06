using NUnit.Framework;
using LeanCloud;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloudTests {
    public class ObjectTests {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public void TestAVObjectConstructor() {
            AVObject obj = new AVObject("Foo");
            Assert.AreEqual("Foo", obj.ClassName);
            Assert.Null(obj.CreatedAt);
            Assert.True(obj.IsDataAvailable);
            Assert.True(obj.IsDirty);
        }

        [Test]
        public void TestAVObjectCreate() {
            AVObject obj = AVObject.CreateWithoutData("Foo", "5d356b1cd5de2b00837162ca");
            Assert.AreEqual("Foo", obj.ClassName);
            Assert.AreEqual("5d356b1cd5de2b00837162ca", obj.ObjectId);
            Assert.Null(obj.CreatedAt);
            Assert.False(obj.IsDataAvailable);
            Assert.False(obj.IsDirty);
        }

        [Test]
        public async Task TestMassiveRequest() {
            await Task.Run(() => {
                for (int i = 0; i < 10; i++) {
                    for (int j = 0; j < 50; j++) {
                        AVObject obj = AVObject.Create("Foo");
                        obj.SaveAsync().ContinueWith(_ => {
                            TestContext.Out.WriteLine($"{obj.ObjectId} saved");
                        });
                    }
                    Thread.Sleep(1000);
                }
            });
        }
    }
}