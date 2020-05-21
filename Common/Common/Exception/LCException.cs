using System;

namespace LeanCloud {
    /// <summary>
    /// LeanCloud 异常
    /// </summary>
    public class LCException : Exception {
        /// <summary>
        /// 错误码
        /// </summary>
        public int Code {
            get; set;
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public new string Message {
            get; set;
        }

        public LCException(int code, string message) {
            Code = code;
            Message = message;
        }

        public override string ToString() {
            return $"{Code} - {Message}";
        }
    }
}
