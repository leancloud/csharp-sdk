namespace LeanCloud.Storage.Internal {
  public class AVSetOperation : IAVFieldOperation {
    public AVSetOperation(object value) {
      Value = value;
    }

    public object Encode() {
      return PointerOrLocalIdEncoder.Instance.Encode(Value);
    }

    public IAVFieldOperation MergeWithPrevious(IAVFieldOperation previous) {
      return this;
    }

    public object Apply(object oldValue, string key) {
      return Value;
    }

    public object Value { get; private set; }
  }
}
