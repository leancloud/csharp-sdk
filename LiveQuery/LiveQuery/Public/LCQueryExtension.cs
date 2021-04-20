using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.LiveQuery {
    /// <summary>
    /// LCQueryExtension is the extension of query for live query.
    /// </summary>
    public static class LCQueryExtension {
        /// <summary>
        /// Subscribes this LCQuery to live query.
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
