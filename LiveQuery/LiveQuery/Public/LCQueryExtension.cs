using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.LiveQuery {
    /// <summary>
    /// LCQueryExtension is the extension of a LCQuery.
    /// </summary>
    public static class LCQueryExtension {
        /// <summary>
        /// Subscribes a LCQuery.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<LCLiveQuery> Subscribe(this LCQuery query) {
            LCLiveQuery liveQuery = new LCLiveQuery {
                Query = query
            };
            await liveQuery.Subscribe();
            return liveQuery;
        }
    }
}
