using System;
using LeanCloud;
using LeanCloud.Common;
using NUnit.Framework;

namespace Realtime.Test {
    public static class Utils {
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
