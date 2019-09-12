using System;

namespace LeanCloud.Storage.Internal {
    public class QueryOperation {
        public string Key {
            get; set;
        }

        public string Op {
            get; set;
        }

        public object Value {
            get; set;
        }


    }
}
