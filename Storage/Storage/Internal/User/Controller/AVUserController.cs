using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace LeanCloud.Storage.Internal {
    public class AVUserController {
        public async Task<IObjectState> SignUpAsync(IObjectState state, IDictionary<string, IAVFieldOperation> operations) {
            var objectJSON = AVObject.ToJSONObjectForSaving(operations);
            var command = new AVCommand {
                Path = "classes/_User",
                Method = HttpMethod.Post,
                Content = objectJSON
            };
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            var serverState = AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
            serverState = serverState.MutatedClone(mutableClone => {
                mutableClone.IsNew = true;
            });
            return serverState;
        }

        public async Task<IObjectState> LogInAsync(string username, string email, string password) {
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
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            var serverState = AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
            serverState = serverState.MutatedClone(mutableClone => {
                mutableClone.IsNew = ret.Item1 == System.Net.HttpStatusCode.Created;
            });
            return serverState;
        }

        public async Task<IObjectState> LogInAsync(string authType, IDictionary<string, object> data, bool failOnNotExist) {
            var authData = new Dictionary<string, object> {
                [authType] = data
            };
            var path = failOnNotExist ? "users?failOnNotExist=true" : "users";
            var command = new AVCommand {
                Path = path,
                Method = HttpMethod.Post,
                Content = new Dictionary<string, object> {
                    { "authData", authData }
                }
            };
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            var serverState = AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
            serverState = serverState.MutatedClone(mutableClone => {
                mutableClone.IsNew = ret.Item1 == System.Net.HttpStatusCode.Created;
            });
            return serverState;
        }

        public async Task<IObjectState> GetUserAsync(string sessionToken) {
            var command = new AVCommand {
                Path = "users/me",
                Method = HttpMethod.Get,
                Headers = new Dictionary<string, string> {
                    { "X-LC-Session", sessionToken }
                }
            };
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            return AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
        }

        public async Task RequestPasswordResetAsync(string email) {
            var command = new AVCommand {
                Path = "requestPasswordReset",
                Method = HttpMethod.Post,
                Content = new Dictionary<string, object> {
                    { "email", email}
                }
            };
            await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
        }

        public async Task<IObjectState> LogInWithParametersAsync(string relativeUrl, IDictionary<string, object> data) {
            var command = new AVCommand {
                Path = relativeUrl,
                Method = HttpMethod.Post,
                Content = data
            };
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            var serverState = AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
            serverState = serverState.MutatedClone(mutableClone => {
                mutableClone.IsNew = ret.Item1 == System.Net.HttpStatusCode.Created;
            });
            return serverState;
        }

        public async Task<IObjectState> UpdatePasswordAsync(string userId, string oldPassword, string newPassword) {
            var command = new AVCommand {
                Path = $"users/{userId}/updatePassword",
                Method = HttpMethod.Put,
                Content = new Dictionary<string, object> {
                    { "old_password", oldPassword },
                    { "new_password", newPassword },
                }
            };
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            return AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
        }

        public async Task<IObjectState> RefreshSessionTokenAsync(string userId) {
            var command = new AVCommand {
                Path = $"users/{userId}/refreshSessionToken",
                Method = HttpMethod.Put
            };
            var ret = await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command);
            var serverState = AVObjectCoder.Instance.Decode(ret.Item2, AVDecoder.Instance);
            return serverState;
        }
    }
}
