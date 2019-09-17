using NUnit.Framework;
using LeanCloud;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloud.Test {
    public class CloudFunctionTest {
        [SetUp]
        public void SetUp() {
            Utils.InitNorthChina();
        }

        [Test]
        public async Task Hello() {
            AVClient.UseProduction = true;
            string result = await AVCloud.CallFunctionAsync<string>("hello", new Dictionary<string, object> {
                { "word", "world" }
            });
            Assert.AreEqual(result, "hello, world");
            TestContext.Out.WriteLine($"resutlt: {result}");
        }

        [Test]
        public async Task GetUsernameInCloud() {
            AVClient.UseProduction = true;
            await AVUser.LogInAsync("111111", "111111");
            string result = await AVCloud.CallFunctionAsync<string>("getUsername");
            Assert.AreEqual(result, "111111");
            TestContext.Out.WriteLine($"resutlt: {result}");
        }
    }
}
