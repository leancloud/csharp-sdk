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
            get; private set;
        }

        public LCException(int code, string message) : base(message) {
            Code = code;
        }

        public override string ToString() {
            return $"{Code} - {Message}";
        }
    }
}
