using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAppRouterController
    {
        AppRouterState Get();
        Task<AppRouterState> QueryAsync(CancellationToken cancellationToken);
        void Clear();
    }
}
