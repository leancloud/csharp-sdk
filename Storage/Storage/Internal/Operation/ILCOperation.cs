using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal.Operation {
    internal interface ILCOperation {
        ILCOperation MergeWithPrevious(ILCOperation previousOp);

        Dictionary<string, object> Encode();

        object Apply(object oldValue, string key);

        IEnumerable GetNewObjectList();
    }
}
