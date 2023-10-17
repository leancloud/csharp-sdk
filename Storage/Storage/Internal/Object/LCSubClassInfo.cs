using System;

namespace LeanCloud.Storage.Internal.Object {
    internal class LCSubclassInfo {
        internal string ClassName {
            get;
        }

        internal Func<LCObject> Constructor {
            get;
        }

        internal string Endpoint {
            get;
        }

        internal LCSubclassInfo(string className, Func<LCObject> constructor, string endpoint = null) {
            ClassName = className;
            Constructor = constructor;
            Endpoint = endpoint;
        }
    }
}
