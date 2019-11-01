using System.Threading.Tasks;
using NUnit.Framework;
using LeanCloud.Common;

namespace Common.Test {
    public class Tests {
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
            TestContext.Out.WriteLine("Set up");
            Logger.LogDelegate += Print;
        }

        [TearDown]
        public void TearDown() {
            TestContext.Out.WriteLine("Tear down");
            Logger.LogDelegate -= Print;
        }

        [Test]
        public async Task AppRouter() {
            var appRouter = new AppRouterController();
            for (int i = 0; i < 100; i++) {
                var state = await appRouter.Get("BMYV4RKSTwo8WSqt8q9ezcWF-gzGzoHsz");
                TestContext.Out.WriteLine(state.ApiServer);
            }
        }
    }
}