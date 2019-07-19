using System;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
  /// <summary>
  /// So here's the deal. We have a lot of internal APIs for AVObject, AVUser, etc.
  ///
  /// These cannot be 'internal' anymore if we are fully modularizing things out, because
  /// they are no longer a part of the same library, especially as we create things like
  /// Installation inside push library.
  ///
  /// So this class contains a bunch of extension methods that can live inside another
  /// namespace, which 'wrap' the intenral APIs that already exist.
  /// </summary>
  public static class AVQueryExtensions {
    public static string GetClassName<T>(this AVQuery<T> query) where T: AVObject {
      return query.ClassName;
    }

    public static IDictionary<String, object> BuildParameters<T>(this AVQuery<T> query) where T: AVObject {
      return query.BuildParameters(false);
    }

    public static object GetConstraint<T>(this AVQuery<T> query, string key) where T : AVObject {
      return query.GetConstraint(key);
    }
  }
}
