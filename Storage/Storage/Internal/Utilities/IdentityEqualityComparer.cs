using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// An equality comparer that uses the object identity (i.e. ReferenceEquals)
    /// rather than .Equals, allowing identity to be used for checking equality in
    /// ISets and IDictionaries.
    /// </summary>
    public class IdentityEqualityComparer<T> : IEqualityComparer<T>
        where T : AVObject {
        public bool Equals(T x, T y) {
            return x.ClassName == y.ClassName &&
                x.ObjectId == y.ObjectId;
        }

        public int GetHashCode(T obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
