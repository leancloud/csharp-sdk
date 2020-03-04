using System;
using LeanCloud;
using LeanCloud.Common;
using NUnit.Framework;

namespace LeanCloud.Test {
    public static class Utils {
        internal static void Print(LogLevel level, string info) {
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
    }
}
