using LeanCloud.Storage;

namespace LeanCloud.Engine {
    public class LCClassHookRequest {
        public LCObject Object {
            get; set;
        }

        public LCUser CurrentUser {
            get; set;
        }
    }
}
