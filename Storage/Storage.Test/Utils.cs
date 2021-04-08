using NUnit.Framework;
using LeanCloud;

namespace Storage.Test {
    public static class Utils {
        internal const string AppId = "ikGGdRE2YcVOemAaRbgp1xGJ-gzGzoHsz";
        internal const string AppKey = "NUKmuRbdAhg1vrb2wexYo1jo";
        internal const string MasterKey = "pyvbNSh5jXsuFQ3C8EgnIdhw";
        internal const string AppServer = "https://ikggdre2.lc-cn-n1-shared.com";

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

        internal static void SetUp() {
            LCLogger.LogDelegate += Print;
            LCApplication.Initialize(AppId, AppKey, AppServer);
        }

        internal static void TearDown() {
            LCLogger.LogDelegate -= Print;
        }
    }
}
