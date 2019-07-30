using NUnit.Framework;
using LeanCloud;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace LeanCloudTests {
    public class ObjectTests {
        [SetUp]
        public void SetUp() {
            AVClient.Initialize(new AVClient.Configuration {
                ApplicationId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz",
                ApplicationKey = "pbf6Nk5seyjilexdpyrPwjSp",
                RTMServer = "https://router-g0-push.avoscloud.com",
            });
            AVClient.HttpLog(TestContext.Out.WriteLine);
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
        public async Task TestHttp() {
            if (SynchronizationContext.Current == null) {
                TestContext.Out.WriteLine("is null");
            }
            TestContext.Out.WriteLine($"current {SynchronizationContext.Current}");
            var client = new HttpClient();
            TestContext.Out.WriteLine($"request at {Thread.CurrentThread.ManagedThreadId}");
            string url = $"{AVClient.CurrentConfiguration.RTMServer}/v1/route?appId={AVClient.CurrentConfiguration.ApplicationId}&secure=1";
            var res = await client.GetAsync(url);
            TestContext.Out.WriteLine($"get at {Thread.CurrentThread.ManagedThreadId}");
            var data = await res.Content.ReadAsStringAsync();
            res.Dispose();
            TestContext.Out.WriteLine($"response at {Thread.CurrentThread.ManagedThreadId}");
            TestContext.Out.WriteLine(data);
            Assert.Pass();   
        }

    }
}