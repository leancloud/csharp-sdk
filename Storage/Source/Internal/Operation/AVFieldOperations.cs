using System;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
  public class AVObjectIdComparer : IEqualityComparer<object> {
    bool IEqualityComparer<object>.Equals(object p1, object p2) {
      var avObj1 = p1 as AVObject;
      var avObj2 = p2 as AVObject;
      if (avObj1 != null && avObj2 != null) {
        return object.Equals(avObj1.ObjectId, avObj2.ObjectId);
      }
      return object.Equals(p1, p2);
    }

    public int GetHashCode(object p) {
      var avObject = p as AVObject;
      if (avObject != null) {
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
