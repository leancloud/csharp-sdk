using System.Collections.Generic;

namespace LeanCloud.Storage.Internal.Query {
    public interface ILCQueryCondition {
        bool Equals(ILCQueryCondition other);
        Dictionary<string, object> Encode();
    }
}
