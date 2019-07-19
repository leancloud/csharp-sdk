using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Storage.Internal {
  public interface IAVQueryController {
    Task<IEnumerable<IObjectState>> FindAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject;

    Task<int> CountAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject;

    Task<IObjectState> FirstAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject;
  }
}
