using System;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
  /// <summary>
  /// A <see cref="AVEncoder"/> that throws an exception if it attempts to encode
  /// a <see cref="AVObject"/>
  /// </summary>
  public class NoObjectsEncoder : AVEncoder {
    // This class isn't really a Singleton, but since it has no state, it's more efficient to get
    // the default instance.
    private static readonly NoObjectsEncoder instance = new NoObjectsEncoder();
    public static NoObjectsEncoder Instance {
      get {
        return instance;
      }
    }

    protected override IDictionary<string, object> EncodeAVObject(AVObject value) {
      throw new ArgumentException("AVObjects not allowed here.");
    }
  }
}
