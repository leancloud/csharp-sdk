using System.Collections.ObjectModel;

namespace LeanCloud.Realtime {
    /// <summary>
    /// LCIMPageResult represents the results of query.
    /// </summary>
    public class LCIMPageResult {
        public ReadOnlyCollection<string> Results {
            get; internal set;
        }

        public string Next {
            get; internal set;
        }
    }
}
