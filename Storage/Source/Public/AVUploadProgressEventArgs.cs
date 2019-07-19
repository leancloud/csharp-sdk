using System;

namespace LeanCloud {
  /// <summary>
  /// Represents upload progress.
  /// </summary>
  public class AVUploadProgressEventArgs : EventArgs {
    public AVUploadProgressEventArgs() { }

    /// <summary>
    /// Gets the progress (a number between 0.0 and 1.0) of an upload.
    /// </summary>
    public double Progress { get; set; }
  }
}
