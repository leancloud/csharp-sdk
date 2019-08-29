using NUnit.Framework;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloudTests {
    public class AppRouterTest {
        [Test]
        public async Task GetServers() {
            var appRouter = new AppRouterController();
            for (int i = 0; i < 1000; i++) {
                var state = await appRouter.Get("BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz");
                TestContext.Out.WriteLine(state.ApiServer);
            }
        }
    }
}
