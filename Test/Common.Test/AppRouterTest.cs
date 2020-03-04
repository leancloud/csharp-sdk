using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LeanCloud.Common;

namespace Common.Test {
    public class AppRouterTest {
        static void Print(LogLevel level, string info) {
            switch (level) {
                case LogLevel.Debug:
                    TestContext.Out.WriteLine($"[DEBUG] {info}");
                    break;
                case LogLevel.Warn:
                    TestContext.Out.WriteLine($"[WARNING] {info}");
                    break;
                case LogLevel.Error:
                    TestContext.Out.WriteLine($"[ERROR] {info}");
                    break;
                default:
                    TestContext.Out.WriteLine(info);
                    break;
            }
        }

        [SetUp]
        public void SetUp() {
            Logger.LogDelegate += Print;
        }

        [TearDown]
        public void TearDown() {
            Logger.LogDelegate -= Print;
        }

        [Test]
        public void ChineseApp() {
            Exception e = Assert.Catch(() => {
                string appId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz";
                AppRouterController appRouter = new AppRouterController(appId, null);
                TestContext.WriteLine("init done");
            });
            TestContext.WriteLine(e.Message);
        }

        [Test]
        public async Task ChineseAppWithDomain() {
            string appId = "BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz";
            string server = "https://bmyv4rks.lc-cn-n1-shared.com";
            AppRouterController appRouterController = new AppRouterController(appId, server);
            AppRouter appRouterState = await appRouterController.Get();
            Assert.AreEqual(appRouterState.ApiServer, server);
            Assert.AreEqual(appRouterState.EngineServer, server);
            Assert.AreEqual(appRouterState.PushServer, server);
            Assert.AreEqual(appRouterState.RTMServer, server);
            Assert.AreEqual(appRouterState.StatsServer, server);
            Assert.AreEqual(appRouterState.PlayServer, server);
        }

        [Test]
        public void InternationalApp() {
            string appId = "BMYV4RKSTwo8WSqt8q9ezcWF-MdYXbMMI";
            _ = new AppRouterController(appId, null);
            TestContext.WriteLine("International app init done");
        }
    }
}