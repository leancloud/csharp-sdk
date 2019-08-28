using NUnit.Framework;
using LeanCloud;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeanCloudTests {
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
            TestContext.Out.WriteLine($"resutlt: {result}");
        }
    }
}
