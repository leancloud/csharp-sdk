using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.LiveQuery
{
    /// <summary>
    /// AVLiveQuery 扩展类
    /// </summary>
    public static class AVLiveQueryExtensions
    {
        /// <summary>
        /// AVQuery 订阅 AVLiveQuery 的扩展方法 
        /// </summary>
        /// <returns>AVLiveQuery 对象</returns>
        /// <param name="query">Query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static async Task<AVLiveQuery<T>> SubscribeAsync<T>(this AVQuery<T> query, CancellationToken cancellationToken = default(CancellationToken)) where T : AVObject {
            var liveQuery = new AVLiveQuery<T>(query);
            liveQuery = await liveQuery.SubscribeAsync();
            return liveQuery;
        }
    }
}
