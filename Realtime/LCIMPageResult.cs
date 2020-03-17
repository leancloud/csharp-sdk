using System.Collections.Generic;

namespace LeanCloud.Realtime {
    public class LCIMPageResult {
        public List<string> Results {
            get; internal set;
        }

        public string Next {
            get; internal set;
        }
    }
}
