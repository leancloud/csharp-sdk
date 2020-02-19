using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// An operation where a field is deleted from the object.
    /// </summary>
    public class AVDeleteOperation : IAVFieldOperation {
        internal static readonly object DeleteToken = new object();
        private static AVDeleteOperation _Instance = new AVDeleteOperation();

        public static AVDeleteOperation Instance {
            get {
                return _Instance;
            }
        }

        private AVDeleteOperation() { }

        public object Encode() {
            return new Dictionary<string, object> {
                {"__op", "Delete"}
            };
        }

        public IAVFieldOperation MergeWithPrevious(IAVFieldOperation previous) {
            return this;
        }

        public object Apply(object oldValue, string key) {
            return DeleteToken;
        }
    }
}
