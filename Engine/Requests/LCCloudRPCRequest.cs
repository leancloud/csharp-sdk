using LeanCloud.Storage;

namespace LeanCloud.Engine {
    public class LCCloudRPCRequest {
        public LCCloudFunctionRequestMeta Meta {
            get; set;
        }

        public object Params {
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
