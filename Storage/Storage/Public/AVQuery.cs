using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;
using System.Net.Http;
using Newtonsoft.Json;

namespace LeanCloud
{

    /// <summary>
    /// The AVQuery class defines a query that is used to fetch AVObjects. The
    /// most common use case is finding all objects that match a query through the
    /// <see cref="FindAsync()"/> method.
    /// </summary>
    /// <example>
    /// This sample code fetches all objects of
    /// class <c>"MyClass"</c>:
    ///
    /// <code>
    /// AVQuery query = new AVQuery("MyClass");
    /// IEnumerable&lt;AVObject&gt; result = await query.FindAsync();
    /// </code>
    ///
    /// A AVQuery can also be used to retrieve a single object whose id is known,
    /// through the <see cref="GetAsync(string)"/> method. For example, this sample code
    /// fetches an object of class <c>"MyClass"</c> and id <c>myId</c>.
    ///
    /// <code>
    /// AVQuery query = new AVQuery("MyClass");
    /// AVObject result = await query.GetAsync(myId);
    /// </code>
    ///
    /// A AVQuery can also be used to count the number of objects that match the
    /// query without retrieving all of those objects. For example, this sample code
    /// counts the number of objects of the class <c>"MyClass"</c>.
    ///
    /// <code>
    /// AVQuery query = new AVQuery("MyClass");
    /// int count = await query.CountAsync();
    /// </code>
    /// </example>
    public class AVQuery<T> : AVQueryPair<AVQuery<T>, T>, IAVQuery
        where T : AVObject
    {
        internal static IAVQueryController QueryController
        {
            get
            {
                return AVPlugins.Instance.QueryController;
            }
        }

        internal static IObjectSubclassingController SubclassingController
        {
            get
            {
                return AVPlugins.Instance.SubclassingController;
            }
        }

        /// <summary>
        /// 调试时可以用来查看最终的发送的查询语句
        /// </summary>
        private string JsonString
        {
            get
            {
                return JsonConvert.SerializeObject(BuildParameters(true));
            }
        }

        /// <summary>
        /// Private constructor for composition of queries. A Source query is required,
        /// but the remaining values can be null if they won't be changed in this
        /// composition.
        /// </summary>
        private AVQuery(AVQuery<T> source,
            IDictionary<string, object> where = null,
            IEnumerable<string> replacementOrderBy = null,
            IEnumerable<string> thenBy = null,
            int? skip = null,
            int? limit = null,
            IEnumerable<string> includes = null,
            IEnumerable<string> selectedKeys = null,
            String redirectClassNameForKey = null)
            : base(source, where, replacementOrderBy, thenBy, skip, limit, includes, selectedKeys, redirectClassNameForKey)
        {

        }

        //internal override AVQuery<T> CreateInstance(
        //    AVQuery<T> source,
        //    IDictionary<string, object> where = null,
        //    IEnumerable<string> replacementOrderBy = null,
        //    IEnumerable<string> thenBy = null,
        //    int? skip = null,
        //    int? limit = null,
        //    IEnumerable<string> includes = null,
        //    IEnumerable<string> selectedKeys = null,
        //    String redirectClassNameForKey = null)
        //{
        //    return new AVQuery<T>(this, where, replacementOrderBy, thenBy, skip, limit, includes, selectedKeys, redirectClassNameForKey);
        //}

        public override AVQuery<T> CreateInstance(
            IDictionary<string, object> where = null,
            IEnumerable<string> replacementOrderBy = null,
            IEnumerable<string> thenBy = null,
            int? skip = null,
            int? limit = null,
            IEnumerable<string> includes = null,
            IEnumerable<string> selectedKeys = null,
            String redirectClassNameForKey = null)
        {
            return new AVQuery<T>(this, where, replacementOrderBy, thenBy, skip, limit, includes, selectedKeys, redirectClassNameForKey);
        }


        /// <summary>
        /// Constructs a query based upon the AVObject subclass used as the generic parameter for the AVQuery.
        /// </summary>
        public AVQuery()
          : this(SubclassingController.GetClassName(typeof(T)))
        {
        }

        /// <summary>
        /// Constructs a query. A default query with no further parameters will retrieve
        /// all <see cref="AVObject"/>s of the provided class.
        /// </summary>
        /// <param name="className">The name of the class to retrieve AVObjects for.</param>
        public AVQuery(string className)
            : base(className)
        {

        }

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <param name="queries">The list of AVQueries to 'or' together.</param>
        /// <returns>A AVQquery that is the 'or' of the passed in queries.</returns>
        public static AVQuery<T> Or(IEnumerable<AVQuery<T>> queries)
        {
            string className = null;
            var orValue = new List<IDictionary<string, object>>();
            // We need to cast it to non-generic IEnumerable because of AOT-limitation
            var nonGenericQueries = (IEnumerable)queries;
            foreach (var obj in nonGenericQueries)
            {
                var q = (AVQuery<T>)obj;
                if (className != null && q.className != className)
                {
                    throw new ArgumentException(
                        "All of the queries in an or query must be on the same class.");
                }
                className = q.className;
                var parameters = q.BuildParameters();
                if (parameters.Count == 0)
                {
                    continue;
                }
                object where;
                if (!parameters.TryGetValue("where", out where) || parameters.Count > 1)
                {
                    throw new ArgumentException(
                        "None of the queries in an or query can have non-filtering clauses");
                }
                orValue.Add(where as IDictionary<string, object>);
            }
            return new AVQuery<T>(new AVQuery<T>(className),
              where: new Dictionary<string, object> {
                  { "$or", orValue}
              });
        }

        /// <summary>
        /// Retrieves a list of AVObjects that satisfy this query from LeanCloud.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of AVObjects that match this query.</returns>
        public override Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken)
        {
            return AVUser.GetCurrentUserAsync().OnSuccess(t =>
            {
                return QueryController.FindAsync<T>(this, t.Result, cancellationToken);
            }).Unwrap().OnSuccess(t =>
            {
                IEnumerable<IObjectState> states = t.Result;
                return (from state in states
                        select AVObject.FromState<T>(state, ClassName));
            });
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single AVObject that satisfies this query, or else null.</returns>
        public override Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            return AVUser.GetCurrentUserAsync().OnSuccess(t =>
            {
                return QueryController.FirstAsync<T>(this, t.Result, cancellationToken);
            }).Unwrap().OnSuccess(t =>
            {
                IObjectState state = t.Result;
                return state == null ? default : AVObject.FromState<T>(state, ClassName);
            });
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single AVObject that satisfies this query.</returns>
        /// <exception cref="AVException">If no results match the query.</exception>
        public override Task<T> FirstAsync(CancellationToken cancellationToken)
        {
            return FirstOrDefaultAsync(cancellationToken).OnSuccess(t =>
            {
                if (t.Result == null)
                {
                    throw new AVException(AVException.ErrorCode.ObjectNotFound,
                      "No results matched the query.");
                }
                return t.Result;
            });
        }

        /// <summary>
        /// Counts the number of objects that match this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of objects that match this query.</returns>
        public override Task<int> CountAsync(CancellationToken cancellationToken)
        {
            return AVUser.GetCurrentUserAsync().OnSuccess(t =>
            {
                return QueryController.CountAsync<T>(this, t.Result, cancellationToken);
            }).Unwrap();
        }

        /// <summary>
        /// Constructs a AVObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The AVObject for the given objectId.</returns>
        public override Task<T> GetAsync(string objectId, CancellationToken cancellationToken)
        {
            AVQuery<T> singleItemQuery = new AVQuery<T>(className)
                .WhereEqualTo("objectId", objectId);
            singleItemQuery = new AVQuery<T>(singleItemQuery, includes: this.includes, selectedKeys: this.selectedKeys, limit: 1);
            return singleItemQuery.FindAsync(cancellationToken).OnSuccess(t =>
            {
                var result = t.Result.FirstOrDefault();
                if (result == null)
                {
                    throw new AVException(AVException.ErrorCode.ObjectNotFound,
                      "Object with the given objectId not found.");
                }
                return result;
            });
        }

        #region CQL
        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql">CQL 语句</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>返回符合条件的对象集合</returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql, CancellationToken cancellationToken)
        {
            var queryString = string.Format("cloudQuery?cql={0}", Uri.EscapeDataString(cql));

            return rebuildObjectFromCloudQueryResult(queryString);
        }

        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql"></param>
        /// <returns></returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql)
        {
            return DoCloudQueryAsync(cql, CancellationToken.None);
        }

        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cqlTeamplate">带有占位符的模板 cql 语句</param>
        /// <param name="pvalues">占位符对应的参数数组</param>
        /// <returns></returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cqlTeamplate, params object[] pvalues)
        {
            string queryStringTemplate = "cloudQuery?cql={0}&pvalues={1}";
            string pSrting = JsonConvert.SerializeObject(pvalues);
            string queryString = string.Format(queryStringTemplate, Uri.EscapeDataString(cqlTeamplate), Uri.EscapeDataString(pSrting));

            return rebuildObjectFromCloudQueryResult(queryString);
        }

        internal static Task<IEnumerable<T>> rebuildObjectFromCloudQueryResult(string queryString)
        {
            var command = new AVCommand {
                Path = queryString,
                Method = HttpMethod.Get
            };
            return AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken: CancellationToken.None).OnSuccess(t =>
            {
                var items = t.Result.Item2["results"] as IList<object>;
                var className = t.Result.Item2["className"].ToString();

                IEnumerable<IObjectState> states = (from item in items
                                                    select AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));

                return (from state in states
                        select AVObject.FromState<T>(state, className));
            });
        }

        #endregion

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is AVQuery<T>))
            {
                return false;
            }

            var other = obj as AVQuery<T>;
            return Object.Equals(this.className, other.ClassName) &&
                   this.where.CollectionsEqual(other.where) &&
                   this.orderBy.CollectionsEqual(other.orderBy) &&
                   this.includes.CollectionsEqual(other.includes) &&
                   this.selectedKeys.CollectionsEqual(other.selectedKeys) &&
                   Object.Equals(this.skip, other.skip) &&
                   Object.Equals(this.limit, other.limit);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
