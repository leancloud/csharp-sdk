using System;

namespace LeanCloud.Storage {
    public class LCException : Exception {
        public int Code {
            get; set;
        }

        public new string Message {
            get; set;
        }

        public LCException(int code, string message) {
            Code = code;
            Message = message;
        }
    }
}
