using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
  /// <summary>
  /// Config controller.
  /// </summary>
  internal class AVConfigController : IAVConfigController {
    private readonly IAVCommandRunner commandRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="AVConfigController"/> class.
    /// </summary>
    public AVConfigController(IAVCommandRunner commandRunner, IStorageController storageController) {
      this.commandRunner = commandRunner;
      CurrentConfigController = new AVCurrentConfigController(storageController);
    }

    public IAVCommandRunner CommandRunner { get; internal set; }
    public IAVCurrentConfigController CurrentConfigController { get; internal set; }

    public Task<AVConfig> FetchConfigAsync(String sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = "config",
                Method = HttpMethod.Post,
            };

      return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(task => {
        cancellationToken.ThrowIfCancellationRequested();
        return new AVConfig(task.Result.Item2);
      }).OnSuccess(task => {
        cancellationToken.ThrowIfCancellationRequested();
        CurrentConfigController.SetCurrentConfigAsync(task.Result);
        return task;
      }).Unwrap();
    }
  }
}
