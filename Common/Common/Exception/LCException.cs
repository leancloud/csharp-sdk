using System;

namespace LeanCloud {
    /// <summary>
    /// LeanCloud Exceptions
    /// </summary>
    public class LCException : Exception {
        /// <summary>
        /// Error code
        /// </summary>
        public int Code {
            get; set;
        }

        /// <summary>
        /// Error message
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
