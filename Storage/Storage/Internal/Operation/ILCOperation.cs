using System.Collections;

namespace LeanCloud.Storage.Internal.Operation {
    public interface ILCOperation {
        ILCOperation MergeWithPrevious(ILCOperation previousOp);

        object Encode();

        object Apply(object oldValue, string key);

        IEnumerable GetNewObjectList();
    }
}
