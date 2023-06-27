using System;

namespace LeanCloud.Engine {
    public class LCEngineException : Exception {
        public int Status { get; set; } = 400;

        public int Code { get; set; }

        public LCEngineException(int code, string message) : base(message) {
            Code = code;
        }

        public LCEngineException(int status, int code, string message) : base(message) {
            Status = status;
            Code = code;
        }
    }
}
