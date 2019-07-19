using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal
{
    public interface IAVObjectController
    {
        //Task<IObjectState> FetchAsync(IObjectState state,
        //    string sessionToken,
        //    CancellationToken cancellationToken);

        Task<IObjectState> FetchAsync(IObjectState state,
            IDictionary<string,object> queryString,
            string sessionToken,
            CancellationToken cancellationToken);

        Task<IObjectState> SaveAsync(IObjectState state,
            IDictionary<string, IAVFieldOperation> operations,
            string sessionToken,
            CancellationToken cancellationToken);

        IList<Task<IObjectState>> SaveAllAsync(IList<IObjectState> states,
            IList<IDictionary<string, IAVFieldOperation>> operationsList,
            string sessionToken,
            CancellationToken cancellationToken);

        Task DeleteAsync(IObjectState state,
            string sessionToken,
            CancellationToken cancellationToken);

        IList<Task> DeleteAllAsync(IList<IObjectState> states,
            string sessionToken,
            CancellationToken cancellationToken);
    }
}
