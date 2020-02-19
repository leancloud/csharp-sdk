using System;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    public class AVObjectIdComparer : IEqualityComparer<object> {
        bool IEqualityComparer<object>.Equals(object p1, object p2) {
            if (p1 is AVObject avObj1 && p2 is AVObject avObj2) {
                return object.Equals(avObj1.ObjectId, avObj2.ObjectId);
            }
            return object.Equals(p1, p2);
        }

        public int GetHashCode(object p) {
            if (p is AVObject avObject) {
                return avObject.ObjectId.GetHashCode();
            }
            return p.GetHashCode();
        }
    }

    static class AVFieldOperations {
        private static AVObjectIdComparer comparer;

        public static IAVFieldOperation Decode(IDictionary<string, object> json) {
            throw new NotImplementedException();
        }

        public static IEqualityComparer<object> AVObjectComparer {
            get {
                if (comparer == null) {
                    comparer = new AVObjectIdComparer();
                }
                return comparer;
            }
        }
    }
}
