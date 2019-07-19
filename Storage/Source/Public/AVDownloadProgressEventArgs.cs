using System;

namespace LeanCloud {
  /// <summary>
  /// Represents download progress.
  /// </summary>
  public class AVDownloadProgressEventArgs : EventArgs {
    public AVDownloadProgressEventArgs() { }

    /// <summary>
    /// Gets the progress (a number between 0.0 and 1.0) of a download.
    /// </summary>
    public double Progress { get; set; }
  }
}
