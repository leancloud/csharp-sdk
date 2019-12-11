using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    /// <summary>
    /// Provides convenience extension methods for working with collections
    /// of AVObjects so that you can easily save and fetch them in batches.
    /// </summary>
    public static class AVExtensions {
        /// <summary>
        /// Saves all of the AVObjects in the enumeration. Equivalent to
        /// calling
        /// <see cref="AVObject.SaveAllAsync{T}(IEnumerable{T}, CancellationToken)"/>.
        /// </summary>
        /// <param name="objects">The objects to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SaveAllAsync<T>(this IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : AVObject {
            return AVObject.SaveAllAsync(objects, cancellationToken);
        }

        /// <summary>
        /// Fetches all of the objects in the enumeration. Equivalent to
        /// calling <see cref="AVObject.FetchAllAsync{T}(IEnumerable{T})"/>.
        /// </summary>
        /// <param name="objects">The objects to save.</param>
        public static Task<IEnumerable<T>> FetchAllAsync<T>(this IEnumerable<T> objects)
          where T : AVObject {
            return AVObject.FetchAllAsync(objects);
        }

        /// <summary>
        /// Fetches all of the objects in the enumeration. Equivalent to
        /// calling
        /// <see cref="AVObject.FetchAllAsync{T}(IEnumerable{T}, CancellationToken)"/>.
        /// </summary>
        /// <param name="objects">The objects to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task<IEnumerable<T>> FetchAllAsync<T>(
            this IEnumerable<T> objects, CancellationToken cancellationToken)
          where T : AVObject {
            return AVObject.FetchAllAsync(objects, cancellationToken);
        }

        /// <summary>
        /// Fetches all of the objects in the enumeration that don't already have
        /// data. Equivalent to calling
        /// <see cref="AVObject.FetchAllIfNeededAsync{T}(IEnumerable{T})"/>.
        /// </summary>
        /// <param name="objects">The objects to fetch.</param>
        public static Task<IEnumerable<T>> FetchAllIfNeededAsync<T>(
            this IEnumerable<T> objects)
          where T : AVObject {
            return AVObject.FetchAllIfNeededAsync(objects);
        }

        /// <summary>
        /// Fetches all of the objects in the enumeration that don't already have
        /// data. Equivalent to calling
        /// <see cref="AVObject.FetchAllIfNeededAsync{T}(IEnumerable{T}, CancellationToken)"/>.
        /// </summary>
        /// <param name="objects">The objects to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task<IEnumerable<T>> FetchAllIfNeededAsync<T>(
            this IEnumerable<T> objects, CancellationToken cancellationToken)
          where T : AVObject {
            return AVObject.FetchAllIfNeededAsync(objects, cancellationToken);
        }

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <typeparam name="T">The type of AVObject being queried.</typeparam>
        /// <param name="source">An initial query to 'or' with additional queries.</param>
        /// <param name="queries">The list of AVQueries to 'or' together.</param>
        /// <returns>A query that is the or of the given queries.</returns>
        public static AVQuery<T> Or<T>(this AVQuery<T> source, params AVQuery<T>[] queries)
            where T : AVObject {
            return AVQuery<T>.Or(queries.Concat(new[] { source }));
        }

        public static Task<T> FetchAsync<T>(this T obj,
            IEnumerable<string> keys = null, IEnumerable<string> includes = null, AVACL includeACL = null,
            CancellationToken cancellationToken = default) where T : AVObject {
            var queryString = new Dictionary<string, object>();
            if (keys != null) {
                var encode = string.Join(",", keys.ToArray());
                queryString.Add("keys", encode);
            }
            if (includes != null) {
                var encode = string.Join(",", includes.ToArray());
                queryString.Add("include", encode);
            }
            if (includeACL != null) {
                queryString.Add("returnACL", includeACL);
            }
            return obj.FetchAsyncInternal(queryString, cancellationToken).OnSuccess(t => (T)t.Result);
        }
    }
}
