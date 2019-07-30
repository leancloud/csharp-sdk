using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LeanCloud.Utilities;

namespace LeanCloud.Storage.Internal {
  public class AVAddOperation : IAVFieldOperation {
    private ReadOnlyCollection<object> objects;
    public AVAddOperation(IEnumerable<object> objects) {
      this.objects = new ReadOnlyCollection<object>(objects.ToList());
    }

    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "Add"},
        {"objects", PointerOrLocalIdEncoder.Instance.Encode(objects)}
      };
    }

    public IAVFieldOperation MergeWithPrevious(IAVFieldOperation previous) {
      if (previous == null) {
        return this;
      }
      if (previous is AVDeleteOperation) {
        return new AVSetOperation(objects.ToList());
      }
      if (previous is AVSetOperation) {
        var setOp = (AVSetOperation)previous;
        var oldList = Conversion.To<IList<object>>(setOp.Value);
        return new AVSetOperation(oldList.Concat(objects).ToList());
      }
      if (previous is AVAddOperation) {
        return new AVAddOperation(((AVAddOperation)previous).Objects.Concat(objects));
      }
      throw new InvalidOperationException("Operation is invalid after previous operation.");
    }

    public object Apply(object oldValue, string key) {
      if (oldValue == null) {
        return objects.ToList();
      }
      var oldList = Conversion.To<IList<object>>(oldValue);
      return oldList.Concat(objects).ToList();
    }

    public IEnumerable<object> Objects {
      get {
        return objects;
      }
    }
  }
}
