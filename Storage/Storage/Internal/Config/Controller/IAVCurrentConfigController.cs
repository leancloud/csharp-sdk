using System;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
  public interface IAVCurrentConfigController {
    /// <summary>
    /// Gets the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    Task<AVConfig> GetCurrentConfigAsync();

    /// <summary>
    /// Sets the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    /// <param name="config">Config.</param>
    Task SetCurrentConfigAsync(AVConfig config);

    /// <summary>
    /// Clears the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    Task ClearCurrentConfigAsync();

    /// <summary>
    /// Clears the current config in memory async.
    /// </summary>
    /// <returns>The current config in memory async.</returns>
    Task ClearCurrentConfigInMemoryAsync();
  }
}
