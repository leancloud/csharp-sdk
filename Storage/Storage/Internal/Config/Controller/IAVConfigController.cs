using System;
using System.Threading.Tasks;
using System.Threading;

namespace LeanCloud.Storage.Internal {
  public interface IAVConfigController {
    /// <summary>
    /// Gets the current config controller.
    /// </summary>
    /// <value>The current config controller.</value>
    IAVCurrentConfigController CurrentConfigController { get; }

    /// <summary>
    /// Fetches the config from the server asynchronously.
    /// </summary>
    /// <returns>The config async.</returns>
    /// <param name="sessionToken">Session token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AVConfig> FetchConfigAsync(String sessionToken, CancellationToken cancellationToken);
  }
}
