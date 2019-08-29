using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAVCloudCodeController
    {
        Task<T> CallFunctionAsync<T>(string name,
            IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken);

        Task<T> RPCFunction<T>(string name, IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken);
    }
}
