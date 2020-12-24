using System;
using LeanCloud;
using NUnit.Framework;

namespace Realtime.Test {
    public static class Utils {
        internal static void SetUp() {
            LCLogger.LogDelegate += Print;
            LCApplication.Initialize("3zWMOXuO9iSdnjXM942i6DdI-gzGzoHsz", "bkwiNq4Tj417eUaHlTWS5sPm", "https://3zwmoxuo.lc-cn-n1-shared.com");
        }

        internal static void TearDown() {
            LCLogger.LogDelegate -= Print;
        }

        internal static void Print(LCLogLevel level, string info) {
            switch (level) {
                case LCLogLevel.Debug:
                    TestContext.Out.WriteLine($"[DEBUG] {DateTime.Now} {info}\n");
                    break;
                case LCLogLevel.Warn:
                    TestContext.Out.WriteLine($"[WARNING] {DateTime.Now} {info}\n");
                    break;
                case LCLogLevel.Error:
                    TestContext.Out.WriteLine($"[ERROR] {DateTime.Now} {info}\n");
                    break;
                default:
                    TestContext.Out.WriteLine(info);
                    break;
            }
        }
    }
}
