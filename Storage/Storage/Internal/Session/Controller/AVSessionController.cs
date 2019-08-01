using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
  public class AVSessionController : IAVSessionController {
    private readonly IAVCommandRunner commandRunner;

    public AVSessionController(IAVCommandRunner commandRunner) {
      this.commandRunner = commandRunner;
    }

    public Task<IObjectState> GetSessionAsync(string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = "sessions/me",
                Method = HttpMethod.Get
            };
      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
      });
    }

    public Task RevokeAsync(string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = "logout",
                Method = HttpMethod.Post
            };
      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
    }

    public Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken) {
            var command = new AVCommand {
                Path = "upgradeToRevocableSession",
                Method = HttpMethod.Post,
            };
      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
      });
    }

    public bool IsRevocableSessionToken(string sessionToken) {
      return sessionToken.Contains("r:");
    }
  }
}
