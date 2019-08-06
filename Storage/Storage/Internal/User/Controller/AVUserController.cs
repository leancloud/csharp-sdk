using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace LeanCloud.Storage.Internal
{
    public class AVUserController : IAVUserController
    {
        private readonly IAVCommandRunner commandRunner;

        public AVUserController(IAVCommandRunner commandRunner)
        {
            this.commandRunner = commandRunner;
        }

        public Task<IObjectState> SignUpAsync(IObjectState state,
            IDictionary<string, IAVFieldOperation> operations,
            CancellationToken cancellationToken)
        {
            var objectJSON = AVObject.ToJSONObjectForSaving(operations);
            var command = new AVCommand {
                Path = "classes/_User",
                Method = HttpMethod.Post,
                Content = objectJSON
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = true;
                });
                return serverState;
            });
        }

        public Task<IObjectState> LogInAsync(string username, string email,
            string password,
            CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>{
                { "password", password}
            };
            if (username != null) {
                data.Add("username", username);
            }
            if (email != null) {
                data.Add("email", email);
            }
            var command = new AVCommand {
                Path = "login",
                Method = HttpMethod.Post,
                Content = data
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

        public Task<IObjectState> LogInAsync(string authType,
            IDictionary<string, object> data,
            bool failOnNotExist,
            CancellationToken cancellationToken)
        {
            var authData = new Dictionary<string, object>();
            authData[authType] = data;
            var path = failOnNotExist ? "users?failOnNotExist=true" : "users";
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Post,
                Content = new Dictionary<string, object> {
                    { "authData", authData}
                }
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

        public Task<IObjectState> GetUserAsync(string sessionToken, CancellationToken cancellationToken)
        {
            var command = new AVCommand {
                Path = "users/me",
                Method = HttpMethod.Get
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
            });
        }

        public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
        {
            var command = new AVCommand {
                Path = "requestPasswordReset",
                Method = HttpMethod.Post,
                Content = new Dictionary<string, object> {
                    { "email", email}
                }
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken);
        }

        public Task<IObjectState> LogInWithParametersAsync(string relativeUrl, IDictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            var command = new AVCommand {
                Path = relativeUrl,
                Method = HttpMethod.Post,
                Content = data
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

        public Task UpdatePasswordAsync(string userId, string sessionToken, string oldPassword, string newPassword, CancellationToken cancellationToken)
        {
            var command = new AVCommand {
                Path = $"users/{userId}/updatePassword",
                Method = HttpMethod.Put,
                Content = new Dictionary<string, object> {
                    { "old_password", oldPassword },
                    { "new_password", newPassword },
                }
            };
            return commandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: cancellationToken);
        }

        public Task<IObjectState> RefreshSessionTokenAsync(string userId, string sessionToken,
            CancellationToken cancellationToken)
        {
            var command = new AVCommand {
                Path = $"users/{userId}/refreshSessionToken",
                Method = HttpMethod.Put
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                return serverState;
            });
        }
    }
}
