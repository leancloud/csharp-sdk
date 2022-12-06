using System;
using LC.Newtonsoft.Json;

namespace LeanCloud.Play {
    public class PlayException : Exception {
        [JsonProperty("reasonCode")]
        public int Code {
            get; private set;
        }

        [JsonProperty("detail")]
        public string Detail {
            get; private set;
        }

        public PlayException() {

        }

        public PlayException(int code, string detail): 
            base(string.Format("{0}, {1}", code, detail)) {
            Code = code;
            Detail = detail;
        }

        public override string ToString() {
            return string.Format("{0}, {1}", Code, Detail);
        }
    }
}
