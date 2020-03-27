using System.Collections.ObjectModel;

namespace LeanCloud.Realtime {
    public class LCIMPageResult {
        public ReadOnlyCollection<string> Results {
            get; internal set;
        }

        public string Next {
            get; internal set;
        }
    }
}
