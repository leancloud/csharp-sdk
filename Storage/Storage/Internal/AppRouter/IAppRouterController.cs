using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAppRouterController
    {
        AppRouterState Get();
        /// <summary>
        /// Start refresh the app router.
        /// </summary>
        /// <returns></returns>
        Task RefreshAsync();
        void Clear();
        /// <summary>
        /// Query the app router.
        /// </summary>
        /// <returns>New AppRouterState</returns>
        Task<AppRouterState> QueryAsync(CancellationToken cancellationToken);
    }
}
