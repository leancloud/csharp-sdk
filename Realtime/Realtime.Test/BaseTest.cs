using NUnit.Framework;
using System.Threading.Tasks;
using LeanCloud;

namespace Realtime.Test {
    public class BaseTest {
        internal const string AppId = "ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz";
        internal const string AppKey = "NUKmuRbdAhg1vrb2wexYo1jo";
        internal const string MasterKey = "pyvbNSh5jXsuFQ3C8EgnIdhw";
        internal const string AppServer = "https://ikggdre2.lc-cn-n1-shared.com";

        internal const string TestPhone = "18888888888";
        internal const string TestSMSCode = "235750";

        [SetUp]
        public virtual Task SetUp() {
            LCLogger.LogDelegate += Print;
            LCApplication.Initialize(AppId, AppKey, AppServer);
            return Task.CompletedTask;
        }

        [TearDown]
        public virtual Task TearDown() {
            LCLogger.LogDelegate -= Print;
            return Task.CompletedTask;
        }

        internal static void Print(LCLogLevel level, string info) {
            switch (level) {
                case LCLogLevel.Debug:
                    TestContext.Out.WriteLine($"[DEBUG] {info}");
                    break;
                case LCLogLevel.Warn:
                    TestContext.Out.WriteLine($"[WARNING] {info}");
                    break;
                case LCLogLevel.Error:
                    TestContext.Out.WriteLine($"[ERROR] {info}");
                    break;
                default:
                    TestContext.Out.WriteLine(info);
                    break;
            }
        }
    }
}
