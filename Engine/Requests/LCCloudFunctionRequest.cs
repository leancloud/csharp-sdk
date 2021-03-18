using System.Collections.Generic;
using LeanCloud.Storage;

namespace LeanCloud.Engine {
    public class LCCloudFunctionRequestMeta {
        public string RemoteAddress {
            get; set;
        }
    }

    public class LCCloudFunctionRequest {
        public LCCloudFunctionRequestMeta Meta {
            get; set;
        }

        public Dictionary<string, object> Params {
            get; set;
        }

        public LCUser User {
            get; set;
        }

        public string SessionToken {
            get; set;
        }
    }
}
