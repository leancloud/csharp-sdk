using System.Threading.Tasks;
using LeanCloud.Storage;

namespace LeanCloud.LiveQuery {
    public static class LCQueryExtension {
        public static async Task<LCLiveQuery> Subscribe(this LCQuery query) {
            LCLiveQuery liveQuery = new LCLiveQuery {
                Query = query
            };
            await liveQuery.Subscribe();
            return liveQuery;
        }
    }
}
