using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAVUserController
    {
        Task<IObjectState> SignUpAsync(IObjectState state,
            IDictionary<string, IAVFieldOperation> operations,
            CancellationToken cancellationToken);

        Task<IObjectState> LogInAsync(string username,
            string email,
            string password,
            CancellationToken cancellationToken);

        Task<IObjectState> LogInWithParametersAsync(string relativeUrl,
            IDictionary<string, object> data,
            CancellationToken cancellationToken);

        Task<IObjectState> LogInAsync(string authType,
            IDictionary<string, object> data,
            bool failOnNotExist,
            CancellationToken cancellationToken);

        Task<IObjectState> GetUserAsync(string sessionToken,
            CancellationToken cancellationToken);

        Task RequestPasswordResetAsync(string email,
            CancellationToken cancellationToken);

        Task UpdatePasswordAsync(string usedId, string sessionToken,
            string oldPassword, string newPassword,
            CancellationToken cancellationToken);

        Task<IObjectState> RefreshSessionTokenAsync(string userId,
            string sessionToken,
            CancellationToken cancellationToken);
    }
}
