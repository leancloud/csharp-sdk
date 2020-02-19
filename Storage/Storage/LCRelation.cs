using System;

namespace LeanCloud.Storage {
    public class LCRelation<T> where T : LCObject {
        public string Key {
            get; set;
        }

        public LCObject Parent {
            get; set;
        }

        public string targetClass {
            get; set;
        }

        public LCRelation() {
        }
    }
}
