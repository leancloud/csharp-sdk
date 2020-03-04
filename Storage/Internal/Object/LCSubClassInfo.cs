using System;

namespace LeanCloud.Storage.Internal.Object {
    internal class LCSubclassInfo {
        internal string ClassName {
            get;
        }

        internal Type Type {
            get;
        }

        internal Func<LCObject> Constructor {
            get;
        }

        internal LCSubclassInfo(string className, Type type, Func<LCObject> constructor) {
            ClassName = className;
            Type = type;
            Constructor = constructor;
        }
    }
}
