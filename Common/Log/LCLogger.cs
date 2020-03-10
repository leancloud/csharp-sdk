using System;

namespace LeanCloud.Common {
    /// <summary>
    /// 日志类
    /// </summary>
    public static class LCLogger {
        /// <summary>
        /// 日志回调接口，方便开发者调试
        /// </summary>
        /// <value>The log delegate.</value>
        public static Action<LCLogLevel, string> LogDelegate {
            get; set;
        }

        public static void Debug(string log) {
            LogDelegate?.Invoke(LCLogLevel.Debug, log);
        }

        public static void Debug(string format, params object[] args) {
            LogDelegate?.Invoke(LCLogLevel.Debug, string.Format(format, args));
        }

        public static void Warn(string log) {
            LogDelegate?.Invoke(LCLogLevel.Warn, log);
        }

        public static void Warn(string format, params object[] args) {
            LogDelegate?.Invoke(LCLogLevel.Warn, string.Format(format, args));
        }

        public static void Error(string log) {
            LogDelegate?.Invoke(LCLogLevel.Error, log);
        }

        public static void Error(string format, params object[] args) {
            LogDelegate?.Invoke(LCLogLevel.Error, string.Format(format, args));
        }
    }
}
