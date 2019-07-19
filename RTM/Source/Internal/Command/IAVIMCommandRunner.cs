using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    public interface IAVIMCommandRunner
    {
        Task<Tuple<int, IDictionary<string, object>>> RunCommandAsync(AVIMCommand command,
            CancellationToken cancellationToken = default(CancellationToken));

        void RunCommand(AVIMCommand command);
    }
}
