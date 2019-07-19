using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

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

            var command = new AVCommand("classes/_User",
                method: "POST",
                data: objectJSON);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
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

            var command = new AVCommand("login",
                method: "POST",
                data: data);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
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
            var command = new AVCommand(path,
                method: "POST",
                data: new Dictionary<string, object> {
                    { "authData", authData}
                });

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
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
            var command = new AVCommand("users/me",
                method: "GET",
                sessionToken: sessionToken,
                data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
            });
        }

        public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
        {
            var command = new AVCommand("requestPasswordReset",
                method: "POST",
                data: new Dictionary<string, object> {
                    { "email", email}
                });

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
        }

        public Task<IObjectState> LogInWithParametersAsync(string relativeUrl, IDictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            var command = new AVCommand(string.Format("{0}", relativeUrl),
                method: "POST",
                data: data);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
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
            var command = new AVCommand(String.Format("users/{0}/updatePassword", userId),
                method: "PUT",
                sessionToken: sessionToken,
                data: new Dictionary<string, object> {
                    {"old_password", oldPassword},
                    {"new_password", newPassword},
                });
            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
        }

        public Task<IObjectState> RefreshSessionTokenAsync(string userId, string sessionToken,
            CancellationToken cancellationToken)
        {
            var command = new AVCommand(String.Format("users/{0}/refreshSessionToken", userId),
                method: "PUT",
                sessionToken: sessionToken,
                data: null);
            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                return serverState;
            });
        }
    }
}
