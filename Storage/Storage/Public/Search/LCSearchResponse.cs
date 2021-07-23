using System.Collections.ObjectModel;

namespace LeanCloud.Storage {
    public class LCSearchResponse<T> where T : LCObject {
        public int Hits {
            get; set;
        }

        public ReadOnlyCollection<T> Results {
            get; set;
        }

        public string Sid {
            get; set;
        }
    }
}
