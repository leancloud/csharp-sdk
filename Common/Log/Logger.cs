using System;

namespace LeanCloud.Common {
    /// <summary>
    /// 日志类
    /// </summary>
    public static class Logger {
        /// <summary>
        /// 日志回调接口，方便开发者调试
        /// </summary>
        /// <value>The log delegate.</value>
        public static Action<LogLevel, string> LogDelegate {
            get; set;
        }

        public static void Debug(string log) {
            LogDelegate?.Invoke(LogLevel.Debug, log);
        }

        public static void Debug(string format, params object[] args) {
            LogDelegate?.Invoke(LogLevel.Debug, string.Format(format, args));
        }

        public static void Warn(string log) {
            LogDelegate?.Invoke(LogLevel.Warn, log);
        }

        public static void Warn(string format, params object[] args) {
            LogDelegate?.Invoke(LogLevel.Warn, string.Format(format, args));
        }

        public static void Error(string log) {
            LogDelegate?.Invoke(LogLevel.Error, log);
        }

        public static void Error(string format, params object[] args) {
            LogDelegate?.Invoke(LogLevel.Error, string.Format(format, args));
        }
    }
}
