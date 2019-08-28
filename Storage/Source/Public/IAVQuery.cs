using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud
{
    /// <summary>
    /// Query 对象的基础接口
    /// </summary>
    public interface IAVQuery
    {

    }

    /// <summary>
    /// LeanCloud 存储对象的接触接口
    /// </summary>
    public interface IAVObject
    {

    }

    public abstract class AVQueryBase<T> : IAVQuery
        where T : IAVObject
    {
        internal string className;
        internal Dictionary<string, object> where;
        internal ReadOnlyCollection<string> orderBy;
        internal ReadOnlyCollection<string> includes;
        internal ReadOnlyCollection<string> selectedKeys;
        internal String redirectClassNameForKey;
        internal int? skip;
        internal int? limit;

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        /// <param name="includeClassName">是否包含 ClassName </param>
        /// <returns></returns>
        public IDictionary<string, object> BuildParameters(bool includeClassName = false)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (where != null)
            {
                result["where"] = PointerOrLocalIdEncoder.Instance.Encode(where);
            }
            if (orderBy != null)
            {
                result["order"] = string.Join(",", orderBy.ToArray());
            }
            if (skip != null)
            {
                result["skip"] = skip.Value;
            }
            if (limit != null)
            {
                result["limit"] = limit.Value;
            }
            if (includes != null)
            {
                result["include"] = string.Join(",", includes.ToArray());
            }
            if (selectedKeys != null)
            {
                result["keys"] = string.Join(",", selectedKeys.ToArray());
            }
            if (includeClassName)
            {
                result["className"] = className;
            }
            if (redirectClassNameForKey != null)
            {
                result["redirectClassNameForKey"] = redirectClassNameForKey;
            }
            return result;
        }

        public virtual Dictionary<string, object> Where
        {
            get
            {
                return this.where;
            }
            set
            {
                this.where = value;
            }
        }

        public virtual IDictionary<string, object> MergeWhere(IDictionary<string, object> primary, IDictionary<string, object> secondary)
        {
            if (secondary == null)
            {
                return primary;
            }
            var newWhere = new Dictionary<string, object>(primary);
            foreach (var pair in secondary)
            {
                var condition = pair.Value as IDictionary<string, object>;
                if (newWhere.ContainsKey(pair.Key))
                {
                    var oldCondition = newWhere[pair.Key] as IDictionary<string, object>;
                    if (oldCondition == null || condition == null)
                    {
                        throw new ArgumentException("More than one where clause for the given key provided.");
                    }
                    var newCondition = new Dictionary<string, object>(oldCondition);
                    foreach (var conditionPair in condition)
                    {
                        if (newCondition.ContainsKey(conditionPair.Key))
                        {
                            throw new ArgumentException("More than one condition for the given key provided.");
                        }
                        newCondition[conditionPair.Key] = conditionPair.Value;
                    }
                    newWhere[pair.Key] = newCondition;
                }
                else
                {
                    newWhere[pair.Key] = pair.Value;
                }
            }
            return newWhere;
        }
    }

    public abstract class AVQueryPair<S, T>
        where S : IAVQuery
        where T : IAVObject

    {
        protected readonly string className;
        protected readonly Dictionary<string, object> where;
        protected readonly ReadOnlyCollection<string> orderBy;
        protected readonly ReadOnlyCollection<string> includes;
        protected readonly ReadOnlyCollection<string> selectedKeys;
        protected readonly string redirectClassNameForKey;
        protected readonly int? skip;
        protected readonly int? limit;

        internal string ClassName { get { return className; } }

        private string relativeUri;
        internal string RelativeUri
        {
            get
            {
                string rtn = string.Empty;
                if (string.IsNullOrEmpty(relativeUri))
                {
                    rtn = "classes/" + Uri.EscapeDataString(this.className);
                }
                else
                {
                    rtn = relativeUri;
                }
                return rtn;
            }
            set
            {
                relativeUri = value;
            }
        }
        public Dictionary<string, object> Condition
        {
            get { return this.where; }
        }

        protected AVQueryPair()
        {

        }

        public abstract S CreateInstance(IDictionary<string, object> where = null,
            IEnumerable<string> replacementOrderBy = null,
            IEnumerable<string> thenBy = null,
            int? skip = null,
            int? limit = null,
            IEnumerable<string> includes = null,
            IEnumerable<string> selectedKeys = null,
            string redirectClassNameForKey = null);

        /// <summary>
        /// Private constructor for composition of queries. A Source query is required,
        /// but the remaining values can be null if they won't be changed in this
        /// composition.
        /// </summary>
        protected AVQueryPair(AVQueryPair<S, T> source,
            IDictionary<string, object> where = null,
            IEnumerable<string> replacementOrderBy = null,
            IEnumerable<string> thenBy = null,
            int? skip = null,
            int? limit = null,
            IEnumerable<string> includes = null,
            IEnumerable<string> selectedKeys = null,
            string redirectClassNameForKey = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException("Source");
            }

            className = source.className;
            this.where = source.where;
            this.orderBy = source.orderBy;
            this.skip = source.skip;
            this.limit = source.limit;
            this.includes = source.includes;
            this.selectedKeys = source.selectedKeys;
            this.redirectClassNameForKey = source.redirectClassNameForKey;

            if (where != null)
            {
                var newWhere = MergeWhereClauses(where);
                this.where = new Dictionary<string, object>(newWhere);
            }

            if (replacementOrderBy != null)
            {
                this.orderBy = new ReadOnlyCollection<string>(replacementOrderBy.ToList());
            }

            if (thenBy != null)
            {
                if (this.orderBy == null)
                {
                    throw new ArgumentException("You must call OrderBy before calling ThenBy.");
                }
                var newOrderBy = new List<string>(this.orderBy);
                newOrderBy.AddRange(thenBy);
                this.orderBy = new ReadOnlyCollection<string>(newOrderBy);
            }

            // Remove duplicates.
            if (this.orderBy != null)
            {
                var newOrderBy = new HashSet<string>(this.orderBy);
                this.orderBy = new ReadOnlyCollection<string>(newOrderBy.ToList<string>());
            }

            if (skip != null)
            {
                this.skip = (this.skip ?? 0) + skip;
            }

            if (limit != null)
            {
                this.limit = limit;
            }

            if (includes != null)
            {
                var newIncludes = MergeIncludes(includes);
                this.includes = new ReadOnlyCollection<string>(newIncludes.ToList());
            }

            if (selectedKeys != null)
            {
                var newSelectedKeys = MergeSelectedKeys(selectedKeys);
                this.selectedKeys = new ReadOnlyCollection<string>(newSelectedKeys.ToList());
            }

            if (redirectClassNameForKey != null)
            {
                this.redirectClassNameForKey = redirectClassNameForKey;
            }
        }

        public AVQueryPair(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className", "Must specify a AVObject class name when creating a AVQuery.");
            }
            this.className = className;
        }

        private HashSet<string> MergeIncludes(IEnumerable<string> includes)
        {
            if (this.includes == null)
            {
                return new HashSet<string>(includes);
            }
            var newIncludes = new HashSet<string>(this.includes);
            foreach (var item in includes)
            {
                newIncludes.Add(item);
            }
            return newIncludes;
        }

        private HashSet<string> MergeSelectedKeys(IEnumerable<string> selectedKeys)
        {
            if (this.selectedKeys == null)
            {
                return new HashSet<string>(selectedKeys);
            }
            var newSelectedKeys = new HashSet<string>(this.selectedKeys);
            foreach (var item in selectedKeys)
            {
                newSelectedKeys.Add(item);
            }
            return newSelectedKeys;
        }

        private IDictionary<string, object> MergeWhereClauses(IDictionary<string, object> where)
        {
            return MergeWhere(this.where, where);
        }

        public virtual IDictionary<string, object> MergeWhere(IDictionary<string, object> primary, IDictionary<string, object> secondary)
        {
            if (secondary == null)
            {
                return primary;
            }
            if (primary == null)
            {
                return secondary;
            }
            var newWhere = new Dictionary<string, object>(primary);
            foreach (var pair in secondary)
            {
                var condition = pair.Value as IDictionary<string, object>;
                if (newWhere.ContainsKey(pair.Key))
                {
                    var oldCondition = newWhere[pair.Key] as IDictionary<string, object>;
                    if (oldCondition == null || condition == null)
                    {
                        throw new ArgumentException("More than one where clause for the given key provided.");
                    }
                    var newCondition = new Dictionary<string, object>(oldCondition);
                    foreach (var conditionPair in condition)
                    {
                        if (newCondition.ContainsKey(conditionPair.Key))
                        {
                            throw new ArgumentException("More than one condition for the given key provided.");
                        }
                        newCondition[conditionPair.Key] = conditionPair.Value;
                    }
                    newWhere[pair.Key] = newCondition;
                }
                else
                {
                    newWhere[pair.Key] = pair.Value;
                }
            }
            return newWhere;
        }

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <param name="queries">The list of AVQueries to 'or' together.</param>
        /// <returns>A AVeQquery that is the 'or' of the passed in queries.</returns>
        public static Q Or<Q, O>(IEnumerable<Q> queries)
            where Q : AVQueryBase<O>
            where O : IAVObject
        {
            string className = null;
            var orValue = new List<IDictionary<string, object>>();
            // We need to cast it to non-generic IEnumerable because of AOT-limitation
            var nonGenericQueries = (IEnumerable)queries;
            Q current = null;
            foreach (var obj in nonGenericQueries)
            {
                var q = (Q)obj;
                current = q;
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
            current.Where = new Dictionary<string, object>()
            {
                {"$or", orValue}
            };
            return current;
        }

        #region Order By

        /// <summary>
        /// Sorts the results in ascending order by the given key.
        /// This will override any existing ordering for the query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S OrderBy(string key)
        {
            return CreateInstance(replacementOrderBy: new List<string> { key });
        }

        /// <summary>
        /// Sorts the results in descending order by the given key.
        /// This will override any existing ordering for the query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S OrderByDescending(string key)
        {
            return CreateInstance(replacementOrderBy: new List<string> { "-" + key });
        }

        /// <summary>
        /// Sorts the results in ascending order by the given key, after previous
        /// ordering has been applied.
        ///
        /// This method can only be called if there is already an <see cref="OrderBy"/>
        /// or <see cref="OrderByDescending"/>
        /// on this query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S ThenBy(string key)
        {
            return CreateInstance(thenBy: new List<string> { key });
        }

        /// <summary>
        /// Sorts the results in descending order by the given key, after previous
        /// ordering has been applied.
        ///
        /// This method can only be called if there is already an <see cref="OrderBy"/>
        /// or <see cref="OrderByDescending"/> on this query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S ThenByDescending(string key)
        {
            return CreateInstance(thenBy: new List<string> { "-" + key });
        }

        #endregion

        /// <summary>
        /// Include nested AVObjects for the provided key. You can use dot notation
        /// to specify which fields in the included objects should also be fetched.
        /// </summary>
        /// <param name="key">The key that should be included.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S Include(string key)
        {
            return CreateInstance(includes: new List<string> { key });
        }

        /// <summary>
        /// Restrict the fields of returned AVObjects to only include the provided key.
        /// If this is called multiple times, then all of the keys specified in each of
        /// the calls will be included.
        /// </summary>
        /// <param name="key">The key that should be included.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S Select(string key)
        {
            return CreateInstance(selectedKeys: new List<string> { key });
        }

        /// <summary>
        /// Skips a number of results before returning. This is useful for pagination
        /// of large queries. Chaining multiple skips together will cause more results
        /// to be skipped.
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S Skip(int count)
        {
            return CreateInstance(skip: count);
        }

        /// <summary>
        /// Controls the maximum number of results that are returned. Setting a negative
        /// limit denotes retrieval without a limit. Chaining multiple limits
        /// results in the last limit specified being used. The default limit is
        /// 100, with a maximum of 1000 results being returned at a time.
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S Limit(int count)
        {
            return CreateInstance(limit: count);
        }

        internal virtual S RedirectClassName(String key)
        {
            return CreateInstance(redirectClassNameForKey: key);
        }

        #region Where

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// contained in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereContainedIn<TIn>(string key, IEnumerable<TIn> values)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$in", values.ToList()}}}
            });
        }

        /// <summary>
        /// Add a constraint to the querey that requires a particular key's value to be
        /// a list containing all of the elements in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereContainsAll<TIn>(string key, IEnumerable<TIn> values)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$all", values.ToList()}}}
            });
        }

        /// <summary>
        /// Adds a constraint for finding string values that contain a provided string.
        /// This will be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="substring">The substring that the value must contain.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereContains(string key, string substring)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$regex", RegexQuote(substring)}}}
            });
        }

        /// <summary>
        /// Adds a constraint for finding objects that do not contain a given key.
        /// </summary>
        /// <param name="key">The key that should not exist.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereDoesNotExist(string key)
        {
            return CreateInstance(where: new Dictionary<string, object>{
                { key, new Dictionary<string, object>{{"$exists", false}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires that a particular key's value
        /// does not match another AVQuery. This only works on keys whose values are
        /// AVObjects or lists of AVObjects.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="query">The query that the value should not match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query)
          where TOther : AVObject
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$notInQuery", query.BuildParameters(true)}}}
            });
        }

        /// <summary>
        /// Adds a constraint for finding string values that end with a provided string.
        /// This will be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="suffix">The substring that the value must end with.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereEndsWith(string key, string suffix)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$regex", RegexQuote(suffix) + "$"}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that the AVObject must contain.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereEqualTo(string key, object value)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, value}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's size to be
        /// equal to the provided size.
        /// </summary>
        /// <returns>The size equal to.</returns>
        /// <param name="key">The key to check.</param>
        /// <param name="size">The value that the size must be.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereSizeEqualTo(string key, uint size)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, uint>{{"$size", size}}}
            });
        }

        /// <summary>
        /// Adds a constraint for finding objects that contain a given key.
        /// </summary>
        /// <param name="key">The key that should exist.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereExists(string key)
        {
            return CreateInstance(where: new Dictionary<string, object>{
                { key, new Dictionary<string, object>{{"$exists", true}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// greater than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereGreaterThan(string key, object value)
        {
            return CreateInstance(where: new Dictionary<string, object>{
                { key, new Dictionary<string, object>{{"$gt", value}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// greater or equal to than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereGreaterThanOrEqualTo(string key, object value)
        {
            return CreateInstance(where: new Dictionary<string, object>{
                { key, new Dictionary<string, object>{{"$gte", value}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// less than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides an upper bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereLessThan(string key, object value)
        {
            return CreateInstance(where: new Dictionary<string, object>{
                { key, new Dictionary<string, object>{{"$lt", value}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// less than or equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereLessThanOrEqualTo(string key, object value)
        {
            return CreateInstance(where: new Dictionary<string, object>{
                { key, new Dictionary<string, object>{{"$lte", value}}}
            });
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="regex">The regular expression pattern to match. The Regex must
        /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
        /// <param name="modifiers">Any of the following supported PCRE modifiers:
        /// <code>i</code> - Case insensitive search
        /// <code>m</code> Search across multiple lines of input</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereMatches(string key, Regex regex, string modifiers)
        {
            if (!regex.Options.HasFlag(RegexOptions.ECMAScript))
            {
                throw new ArgumentException(
                  "Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.");
            }
            return CreateInstance(where: new Dictionary<string, object> {
                { key, EncodeRegex(regex, modifiers)}
            });
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="regex">The regular expression pattern to match. The Regex must
        /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereMatches(string key, Regex regex)
        {
            return WhereMatches(key, regex, null);
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="pattern">The PCRE regular expression pattern to match.</param>
        /// <param name="modifiers">Any of the following supported PCRE modifiers:
        /// <code>i</code> - Case insensitive search
        /// <code>m</code> Search across multiple lines of input</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereMatches(string key, string pattern, string modifiers = null)
        {
            return WhereMatches(key, new Regex(pattern, RegexOptions.ECMAScript), modifiers);
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="pattern">The PCRE regular expression pattern to match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereMatches(string key, string pattern)
        {
            return WhereMatches(key, pattern, null);
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value
        /// to match a value for a key in the results of another AVQuery.
        /// </summary>
        /// <param name="key">The key whose value is being checked.</param>
        /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
        /// <param name="query">The subquery to run</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereMatchesKeyInQuery<TOther>(string key,
          string keyInQuery,
          AVQuery<TOther> query) where TOther : AVObject
        {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(true)},
                { "key", keyInQuery}
            };
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$select", parameters}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value
        /// does not match any value for a key in the results of another AVQuery.
        /// </summary>
        /// <param name="key">The key whose value is being checked.</param>
        /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
        /// <param name="query">The subquery to run</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereDoesNotMatchesKeyInQuery<TOther>(string key,
          string keyInQuery,
          AVQuery<TOther> query) where TOther : AVObject
        {
            var parameters = new Dictionary<string, object> {
                { "query", query.BuildParameters(true)},
                { "key", keyInQuery}
            };
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$dontSelect", parameters}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires that a particular key's value
        /// matches another AVQuery. This only works on keys whose values are
        /// AVObjects or lists of AVObjects.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="query">The query that the value should match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query)
          where TOther : AVObject
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$inQuery", query.BuildParameters(true)}}}
            });
        }

        /// <summary>
        /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
        /// values are near the given point.
        /// </summary>
        /// <param name="key">The key that the AVGeoPoint is stored in.</param>
        /// <param name="point">The reference AVGeoPoint.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereNear(string key, AVGeoPoint point)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$nearSphere", point}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// contained in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$nin", values.ToList()}}}
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value not
        /// to be equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that that must not be equalled.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereNotEqualTo(string key, object value)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$ne", value}}}
            });
        }

        /// <summary>
        /// Adds a constraint for finding string values that start with the provided string.
        /// This query will use the backend index, so it will be fast even with large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="suffix">The substring that the value must start with.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereStartsWith(string key, string suffix)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$regex", "^" + RegexQuote(suffix)}}}
            });
        }

        /// <summary>
        /// Add a constraint to the query that requires a particular key's coordinates to be
        /// contained within a given rectangular geographic bounding box.
        /// </summary>
        /// <param name="key">The key to be constrained.</param>
        /// <param name="southwest">The lower-left inclusive corner of the box.</param>
        /// <param name="northeast">The upper-right inclusive corner of the box.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereWithinGeoBox(string key,
          AVGeoPoint southwest,
          AVGeoPoint northeast)
        {

            return this.CreateInstance(where: new Dictionary<string, object>
            {
                {
                    key,
                    new Dictionary<string, object>
                    {
                        {
                            "$within",
                            new Dictionary<string, object> {
                                { "$box", new[] {southwest, northeast}}
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
        /// values are near the given point and within the maximum distance given.
        /// </summary>
        /// <param name="key">The key that the AVGeoPoint is stored in.</param>
        /// <param name="point">The reference AVGeoPoint.</param>
        /// <param name="maxDistance">The maximum distance (in radians) of results to return.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public virtual S WhereWithinDistance(
            string key, AVGeoPoint point, AVGeoDistance maxDistance)
        {
            var nearWhere = new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$nearSphere", point}}}
            };
            var mergedWhere = MergeWhere(nearWhere, new Dictionary<string, object> {
                { key, new Dictionary<string, object>{{"$maxDistance", maxDistance.Radians}}}
            });
            return CreateInstance(where: mergedWhere);
        }

        internal virtual S WhereRelatedTo(AVObject parent, string key)
        {
            return CreateInstance(where: new Dictionary<string, object> {
                {
                    "$relatedTo",
                    new Dictionary<string, object> {
                        { "object", parent},
                        { "key", key}
                    }
                }
            });
        }

        #endregion

        /// <summary>
        /// Retrieves a list of AVObjects that satisfy this query from LeanCloud.
        /// </summary>
        /// <returns>The list of AVObjects that match this query.</returns>
        public virtual Task<IEnumerable<T>> FindAsync()
        {
            return FindAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves a list of AVObjects that satisfy this query from LeanCloud.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of AVObjects that match this query.</returns>
        public abstract Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken);


        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <returns>A single AVObject that satisfies this query, or else null.</returns>
        public virtual Task<T> FirstOrDefaultAsync()
        {
            return FirstOrDefaultAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single AVObject that satisfies this query, or else null.</returns>
        public abstract Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <returns>A single AVObject that satisfies this query.</returns>
        /// <exception cref="AVException">If no results match the query.</exception>
        public virtual Task<T> FirstAsync()
        {
            return FirstAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single AVObject that satisfies this query.</returns>
        /// <exception cref="AVException">If no results match the query.</exception>
        public abstract Task<T> FirstAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Counts the number of objects that match this query.
        /// </summary>
        /// <returns>The number of objects that match this query.</returns>
        public virtual Task<int> CountAsync()
        {
            return CountAsync(CancellationToken.None);
        }

        /// <summary>
        /// Counts the number of objects that match this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of objects that match this query.</returns>
        public abstract Task<int> CountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Constructs a AVObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
        /// <returns>The AVObject for the given objectId.</returns>
        public virtual Task<T> GetAsync(string objectId)
        {
            return GetAsync(objectId, CancellationToken.None);
        }

        /// <summary>
        /// Constructs a AVObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The AVObject for the given objectId.</returns>
        public abstract Task<T> GetAsync(string objectId, CancellationToken cancellationToken);

        internal object GetConstraint(string key)
        {
            return where == null ? null : where.GetOrDefault(key, null);
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        /// <param name="includeClassName">是否包含 ClassName </param>
        /// <returns></returns>
        public IDictionary<string, object> BuildParameters(bool includeClassName = false)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (where != null)
            {
                result["where"] = PointerOrLocalIdEncoder.Instance.Encode(where);
            }
            if (orderBy != null)
            {
                result["order"] = string.Join(",", orderBy.ToArray());
            }
            if (skip != null)
            {
                result["skip"] = skip.Value;
            }
            if (limit != null)
            {
                result["limit"] = limit.Value;
            }
            if (includes != null)
            {
                result["include"] = string.Join(",", includes.ToArray());
            }
            if (selectedKeys != null)
            {
                result["keys"] = string.Join(",", selectedKeys.ToArray());
            }
            if (includeClassName)
            {
                result["className"] = className;
            }
            if (redirectClassNameForKey != null)
            {
                result["redirectClassNameForKey"] = redirectClassNameForKey;
            }
            return result;
        }

        private string RegexQuote(string input)
        {
            return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
        }

        private string GetRegexOptions(Regex regex, string modifiers)
        {
            string result = modifiers ?? "";
            if (regex.Options.HasFlag(RegexOptions.IgnoreCase) && !modifiers.Contains("i"))
            {
                result += "i";
            }
            if (regex.Options.HasFlag(RegexOptions.Multiline) && !modifiers.Contains("m"))
            {
                result += "m";
            }
            return result;
        }

        private IDictionary<string, object> EncodeRegex(Regex regex, string modifiers)
        {
            var options = GetRegexOptions(regex, modifiers);
            var dict = new Dictionary<string, object>();
            dict["$regex"] = regex.ToString();
            if (!string.IsNullOrEmpty(options))
            {
                dict["$options"] = options;
            }
            return dict;
        }
    }

    //public abstract class AVQueryBase<S, T> : IAVQueryTuple<S, T>
    //    where T : IAVObject
    //{
    //    protected readonly string className;
    //    protected readonly Dictionary<string, object> where;
    //    protected readonly ReadOnlyCollection<string> orderBy;
    //    protected readonly ReadOnlyCollection<string> includes;
    //    protected readonly ReadOnlyCollection<string> selectedKeys;
    //    protected readonly String redirectClassNameForKey;
    //    protected readonly int? skip;
    //    protected readonly int? limit;

    //    internal string ClassName { get { return className; } }

    //    private string relativeUri;
    //    internal string RelativeUri
    //    {
    //        get
    //        {
    //            string rtn = string.Empty;
    //            if (string.IsNullOrEmpty(relativeUri))
    //            {
    //                rtn = "classes/" + Uri.EscapeDataString(this.className);
    //            }
    //            else
    //            {
    //                rtn = relativeUri;
    //            }
    //            return rtn;
    //        }
    //        set
    //        {
    //            relativeUri = value;
    //        }
    //    }
    //    public Dictionary<string, object> Condition
    //    {
    //        get { return this.where; }
    //    }

    //    protected AVQueryBase()
    //    {

    //    }

    //    internal abstract S CreateInstance(AVQueryBase<S, T> source,
    //        IDictionary<string, object> where = null,
    //        IEnumerable<string> replacementOrderBy = null,
    //        IEnumerable<string> thenBy = null,
    //        int? skip = null,
    //        int? limit = null,
    //        IEnumerable<string> includes = null,
    //        IEnumerable<string> selectedKeys = null,
    //        String redirectClassNameForKey = null);

    //    /// <summary>
    //    /// Private constructor for composition of queries. A Source query is required,
    //    /// but the remaining values can be null if they won't be changed in this
    //    /// composition.
    //    /// </summary>
    //    protected AVQueryBase(AVQueryBase<S, T> source,
    //        IDictionary<string, object> where = null,
    //        IEnumerable<string> replacementOrderBy = null,
    //        IEnumerable<string> thenBy = null,
    //        int? skip = null,
    //        int? limit = null,
    //        IEnumerable<string> includes = null,
    //        IEnumerable<string> selectedKeys = null,
    //        String redirectClassNameForKey = null)
    //    {
    //        if (source == null)
    //        {
    //            throw new ArgumentNullException("Source");
    //        }

    //        className = source.className;
    //        this.where = source.where;
    //        this.orderBy = source.orderBy;
    //        this.skip = source.skip;
    //        this.limit = source.limit;
    //        this.includes = source.includes;
    //        this.selectedKeys = source.selectedKeys;
    //        this.redirectClassNameForKey = source.redirectClassNameForKey;

    //        if (where != null)
    //        {
    //            var newWhere = MergeWhereClauses(where);
    //            this.where = new Dictionary<string, object>(newWhere);
    //        }

    //        if (replacementOrderBy != null)
    //        {
    //            this.orderBy = new ReadOnlyCollection<string>(replacementOrderBy.ToList());
    //        }

    //        if (thenBy != null)
    //        {
    //            if (this.orderBy == null)
    //            {
    //                throw new ArgumentException("You must call OrderBy before calling ThenBy.");
    //            }
    //            var newOrderBy = new List<string>(this.orderBy);
    //            newOrderBy.AddRange(thenBy);
    //            this.orderBy = new ReadOnlyCollection<string>(newOrderBy);
    //        }

    //        // Remove duplicates.
    //        if (this.orderBy != null)
    //        {
    //            var newOrderBy = new HashSet<string>(this.orderBy);
    //            this.orderBy = new ReadOnlyCollection<string>(newOrderBy.ToList<string>());
    //        }

    //        if (skip != null)
    //        {
    //            this.skip = (this.skip ?? 0) + skip;
    //        }

    //        if (limit != null)
    //        {
    //            this.limit = limit;
    //        }

    //        if (includes != null)
    //        {
    //            var newIncludes = MergeIncludes(includes);
    //            this.includes = new ReadOnlyCollection<string>(newIncludes.ToList());
    //        }

    //        if (selectedKeys != null)
    //        {
    //            var newSelectedKeys = MergeSelectedKeys(selectedKeys);
    //            this.selectedKeys = new ReadOnlyCollection<string>(newSelectedKeys.ToList());
    //        }

    //        if (redirectClassNameForKey != null)
    //        {
    //            this.redirectClassNameForKey = redirectClassNameForKey;
    //        }
    //    }

    //    public AVQueryBase(string className)
    //    {
    //        if (string.IsNullOrEmpty(className))
    //        {
    //            throw new ArgumentNullException("className", "Must specify a AVObject class name when creating a AVQuery.");
    //        }
    //        this.className = className;
    //    }

    //    private HashSet<string> MergeIncludes(IEnumerable<string> includes)
    //    {
    //        if (this.includes == null)
    //        {
    //            return new HashSet<string>(includes);
    //        }
    //        var newIncludes = new HashSet<string>(this.includes);
    //        foreach (var item in includes)
    //        {
    //            newIncludes.Add(item);
    //        }
    //        return newIncludes;
    //    }

    //    private HashSet<String> MergeSelectedKeys(IEnumerable<String> selectedKeys)
    //    {
    //        if (this.selectedKeys == null)
    //        {
    //            return new HashSet<string>(selectedKeys);
    //        }
    //        var newSelectedKeys = new HashSet<String>(this.selectedKeys);
    //        foreach (var item in selectedKeys)
    //        {
    //            newSelectedKeys.Add(item);
    //        }
    //        return newSelectedKeys;
    //    }

    //    private IDictionary<string, object> MergeWhereClauses(IDictionary<string, object> where)
    //    {
    //        return MergeWhere(this.where, where);
    //    }

    //    public virtual IDictionary<string, object> MergeWhere(IDictionary<string, object> primary, IDictionary<string, object> secondary)
    //    {
    //        if (secondary == null)
    //        {
    //            return primary;
    //        }
    //        var newWhere = new Dictionary<string, object>(primary);
    //        foreach (var pair in secondary)
    //        {
    //            var condition = pair.Value as IDictionary<string, object>;
    //            if (newWhere.ContainsKey(pair.Key))
    //            {
    //                var oldCondition = newWhere[pair.Key] as IDictionary<string, object>;
    //                if (oldCondition == null || condition == null)
    //                {
    //                    throw new ArgumentException("More than one where clause for the given key provided.");
    //                }
    //                var newCondition = new Dictionary<string, object>(oldCondition);
    //                foreach (var conditionPair in condition)
    //                {
    //                    if (newCondition.ContainsKey(conditionPair.Key))
    //                    {
    //                        throw new ArgumentException("More than one condition for the given key provided.");
    //                    }
    //                    newCondition[conditionPair.Key] = conditionPair.Value;
    //                }
    //                newWhere[pair.Key] = newCondition;
    //            }
    //            else
    //            {
    //                newWhere[pair.Key] = pair.Value;
    //            }
    //        }
    //        return newWhere;
    //    }

    //    ///// <summary>
    //    ///// Constructs a query that is the or of the given queries.
    //    ///// </summary>
    //    ///// <param name="queries">The list of AVQueries to 'or' together.</param>
    //    ///// <returns>A AVQquery that is the 'or' of the passed in queries.</returns>
    //    //public static AVQuery<T> Or(IEnumerable<AVQuery<T>> queries)
    //    //{
    //    //    string className = null;
    //    //    var orValue = new List<IDictionary<string, object>>();
    //    //    // We need to cast it to non-generic IEnumerable because of AOT-limitation
    //    //    var nonGenericQueries = (IEnumerable)queries;
    //    //    foreach (var obj in nonGenericQueries)
    //    //    {
    //    //        var q = (AVQuery<T>)obj;
    //    //        if (className != null && q.className != className)
    //    //        {
    //    //            throw new ArgumentException(
    //    //                "All of the queries in an or query must be on the same class.");
    //    //        }
    //    //        className = q.className;
    //    //        var parameters = q.BuildParameters();
    //    //        if (parameters.Count == 0)
    //    //        {
    //    //            continue;
    //    //        }
    //    //        object where;
    //    //        if (!parameters.TryGetValue("where", out where) || parameters.Count > 1)
    //    //        {
    //    //            throw new ArgumentException(
    //    //                "None of the queries in an or query can have non-filtering clauses");
    //    //        }
    //    //        orValue.Add(where as IDictionary<string, object>);
    //    //    }
    //    //    return new AVQuery<T>(new AVQuery<T>(className),
    //    //      where: new Dictionary<string, object> {
    //    //  {"$or", orValue}
    //    //      });
    //    //}

    //    #region Order By

    //    /// <summary>
    //    /// Sorts the results in ascending order by the given key.
    //    /// This will override any existing ordering for the query.
    //    /// </summary>
    //    /// <param name="key">The key to order by.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S OrderBy(string key)
    //    {
    //        return CreateInstance( replacementOrderBy: new List<string> { key });
    //    }

    //    /// <summary>
    //    /// Sorts the results in descending order by the given key.
    //    /// This will override any existing ordering for the query.
    //    /// </summary>
    //    /// <param name="key">The key to order by.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S OrderByDescending(string key)
    //    {
    //        return CreateInstance( replacementOrderBy: new List<string> { "-" + key });
    //    }

    //    /// <summary>
    //    /// Sorts the results in ascending order by the given key, after previous
    //    /// ordering has been applied.
    //    ///
    //    /// This method can only be called if there is already an <see cref="OrderBy"/>
    //    /// or <see cref="OrderByDescending"/>
    //    /// on this query.
    //    /// </summary>
    //    /// <param name="key">The key to order by.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S ThenBy(string key)
    //    {
    //        return CreateInstance( thenBy: new List<string> { key });
    //    }

    //    /// <summary>
    //    /// Sorts the results in descending order by the given key, after previous
    //    /// ordering has been applied.
    //    ///
    //    /// This method can only be called if there is already an <see cref="OrderBy"/>
    //    /// or <see cref="OrderByDescending"/> on this query.
    //    /// </summary>
    //    /// <param name="key">The key to order by.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S ThenByDescending(string key)
    //    {
    //        return CreateInstance( thenBy: new List<string> { "-" + key });
    //    }

    //    #endregion

    //    /// <summary>
    //    /// Include nested AVObjects for the provided key. You can use dot notation
    //    /// to specify which fields in the included objects should also be fetched.
    //    /// </summary>
    //    /// <param name="key">The key that should be included.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S Include(string key)
    //    {
    //        return CreateInstance( includes: new List<string> { key });
    //    }

    //    /// <summary>
    //    /// Restrict the fields of returned AVObjects to only include the provided key.
    //    /// If this is called multiple times, then all of the keys specified in each of
    //    /// the calls will be included.
    //    /// </summary>
    //    /// <param name="key">The key that should be included.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S Select(string key)
    //    {
    //        return CreateInstance( selectedKeys: new List<string> { key });
    //    }

    //    /// <summary>
    //    /// Skips a number of results before returning. This is useful for pagination
    //    /// of large queries. Chaining multiple skips together will cause more results
    //    /// to be skipped.
    //    /// </summary>
    //    /// <param name="count">The number of results to skip.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S Skip(int count)
    //    {
    //        return CreateInstance( skip: count);
    //    }

    //    /// <summary>
    //    /// Controls the maximum number of results that are returned. Setting a negative
    //    /// limit denotes retrieval without a limit. Chaining multiple limits
    //    /// results in the last limit specified being used. The default limit is
    //    /// 100, with a maximum of 1000 results being returned at a time.
    //    /// </summary>
    //    /// <param name="count">The maximum number of results to return.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S Limit(int count)
    //    {
    //        return CreateInstance( limit: count);
    //    }

    //    internal virtual S RedirectClassName(String key)
    //    {
    //        return CreateInstance( redirectClassNameForKey: key);
    //    }

    //    #region Where

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// contained in the provided list of values.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="values">The values that will match.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereContainedIn<TIn>(string key, IEnumerable<TIn> values)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$in", values.ToList()}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Add a constraint to the querey that requires a particular key's value to be
    //    /// a list containing all of the elements in the provided list of values.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="values">The values that will match.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereContainsAll<TIn>(string key, IEnumerable<TIn> values)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$all", values.ToList()}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint for finding string values that contain a provided string.
    //    /// This will be slow for large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="substring">The substring that the value must contain.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereContains(string key, string substring)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$regex", RegexQuote(substring)}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint for finding objects that do not contain a given key.
    //    /// </summary>
    //    /// <param name="key">The key that should not exist.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereDoesNotExist(string key)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object>{
    //            { key, new Dictionary<string, object>{{"$exists", false}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires that a particular key's value
    //    /// does not match another AVQuery. This only works on keys whose values are
    //    /// AVObjects or lists of AVObjects.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="query">The query that the value should not match.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query)
    //      where TOther : AVObject
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$notInQuery", query.BuildParameters(true)}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint for finding string values that end with a provided string.
    //    /// This will be slow for large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="suffix">The substring that the value must end with.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereEndsWith(string key, string suffix)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$regex", RegexQuote(suffix) + "$"}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// equal to the provided value.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="value">The value that the AVObject must contain.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereEqualTo(string key, object value)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, value}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's size to be
    //    /// equal to the provided size.
    //    /// </summary>
    //    /// <returns>The size equal to.</returns>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="size">The value that the size must be.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereSizeEqualTo(string key, uint size)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, uint>{{"$size", size}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint for finding objects that contain a given key.
    //    /// </summary>
    //    /// <param name="key">The key that should exist.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereExists(string key)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object>{
    //            { key, new Dictionary<string, object>{{"$exists", true}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// greater than the provided value.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="value">The value that provides a lower bound.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereGreaterThan(string key, object value)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object>{
    //            { key, new Dictionary<string, object>{{"$gt", value}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// greater or equal to than the provided value.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="value">The value that provides a lower bound.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereGreaterThanOrEqualTo(string key, object value)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object>{
    //            { key, new Dictionary<string, object>{{"$gte", value}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// less than the provided value.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="value">The value that provides an upper bound.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereLessThan(string key, object value)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object>{
    //            { key, new Dictionary<string, object>{{"$lt", value}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// less than or equal to the provided value.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="value">The value that provides a lower bound.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereLessThanOrEqualTo(string key, object value)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object>{
    //            { key, new Dictionary<string, object>{{"$lte", value}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a regular expression constraint for finding string values that match the provided
    //    /// regular expression. This may be slow for large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="regex">The regular expression pattern to match. The Regex must
    //    /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
    //    /// <param name="modifiers">Any of the following supported PCRE modifiers:
    //    /// <code>i</code> - Case insensitive search
    //    /// <code>m</code> Search across multiple lines of input</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereMatches(string key, Regex regex, string modifiers)
    //    {
    //        if (!regex.Options.HasFlag(RegexOptions.ECMAScript))
    //        {
    //            throw new ArgumentException(
    //              "Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.");
    //        }
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, EncodeRegex(regex, modifiers)}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a regular expression constraint for finding string values that match the provided
    //    /// regular expression. This may be slow for large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="regex">The regular expression pattern to match. The Regex must
    //    /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereMatches(string key, Regex regex)
    //    {
    //        return WhereMatches(key, regex, null);
    //    }

    //    /// <summary>
    //    /// Adds a regular expression constraint for finding string values that match the provided
    //    /// regular expression. This may be slow for large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="pattern">The PCRE regular expression pattern to match.</param>
    //    /// <param name="modifiers">Any of the following supported PCRE modifiers:
    //    /// <code>i</code> - Case insensitive search
    //    /// <code>m</code> Search across multiple lines of input</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereMatches(string key, string pattern, string modifiers = null)
    //    {
    //        return WhereMatches(key, new Regex(pattern, RegexOptions.ECMAScript), modifiers);
    //    }

    //    /// <summary>
    //    /// Adds a regular expression constraint for finding string values that match the provided
    //    /// regular expression. This may be slow for large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="pattern">The PCRE regular expression pattern to match.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereMatches(string key, string pattern)
    //    {
    //        return WhereMatches(key, pattern, null);
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value
    //    /// to match a value for a key in the results of another AVQuery.
    //    /// </summary>
    //    /// <param name="key">The key whose value is being checked.</param>
    //    /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
    //    /// <param name="query">The subquery to run</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereMatchesKeyInQuery<TOther>(string key,
    //      string keyInQuery,
    //      AVQuery<TOther> query) where TOther : AVObject
    //    {
    //        var parameters = new Dictionary<string, object> {
    //            { "query", query.BuildParameters(true)},
    //            { "key", keyInQuery}
    //        };
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$select", parameters}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value
    //    /// does not match any value for a key in the results of another AVQuery.
    //    /// </summary>
    //    /// <param name="key">The key whose value is being checked.</param>
    //    /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
    //    /// <param name="query">The subquery to run</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereDoesNotMatchesKeyInQuery<TOther>(string key,
    //      string keyInQuery,
    //      AVQuery<TOther> query) where TOther : AVObject
    //    {
    //        var parameters = new Dictionary<string, object> {
    //            { "query", query.BuildParameters(true)},
    //            { "key", keyInQuery}
    //        };
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$dontSelect", parameters}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires that a particular key's value
    //    /// matches another AVQuery. This only works on keys whose values are
    //    /// AVObjects or lists of AVObjects.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="query">The query that the value should match.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query)
    //      where TOther : AVObject
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$inQuery", query.BuildParameters(true)}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
    //    /// values are near the given point.
    //    /// </summary>
    //    /// <param name="key">The key that the AVGeoPoint is stored in.</param>
    //    /// <param name="point">The reference AVGeoPoint.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereNear(string key, AVGeoPoint point)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$nearSphere", point}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value to be
    //    /// contained in the provided list of values.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="values">The values that will match.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$nin", values.ToList()}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint to the query that requires a particular key's value not
    //    /// to be equal to the provided value.
    //    /// </summary>
    //    /// <param name="key">The key to check.</param>
    //    /// <param name="value">The value that that must not be equalled.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereNotEqualTo(string key, object value)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$ne", value}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a constraint for finding string values that start with the provided string.
    //    /// This query will use the backend index, so it will be fast even with large data sets.
    //    /// </summary>
    //    /// <param name="key">The key that the string to match is stored in.</param>
    //    /// <param name="suffix">The substring that the value must start with.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereStartsWith(string key, string suffix)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$regex", "^" + RegexQuote(suffix)}}}
    //        });
    //    }

    //    /// <summary>
    //    /// Add a constraint to the query that requires a particular key's coordinates to be
    //    /// contained within a given rectangular geographic bounding box.
    //    /// </summary>
    //    /// <param name="key">The key to be constrained.</param>
    //    /// <param name="southwest">The lower-left inclusive corner of the box.</param>
    //    /// <param name="northeast">The upper-right inclusive corner of the box.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereWithinGeoBox(string key,
    //      AVGeoPoint southwest,
    //      AVGeoPoint northeast)
    //    {

    //        return this.CreateInstance( where: new Dictionary<string, object>
    //        {
    //            {
    //                key,
    //                new Dictionary<string, object>
    //                {
    //                    {
    //                        "$within",
    //                        new Dictionary<string, object> {
    //                            { "$box", new[] {southwest, northeast}}
    //                        }
    //                    }
    //                }
    //            }
    //        });
    //    }

    //    /// <summary>
    //    /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
    //    /// values are near the given point and within the maximum distance given.
    //    /// </summary>
    //    /// <param name="key">The key that the AVGeoPoint is stored in.</param>
    //    /// <param name="point">The reference AVGeoPoint.</param>
    //    /// <param name="maxDistance">The maximum distance (in radians) of results to return.</param>
    //    /// <returns>A new query with the additional constraint.</returns>
    //    public virtual S WhereWithinDistance(
    //        string key, AVGeoPoint point, AVGeoDistance maxDistance)
    //    {
    //        var nearWhere = new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$nearSphere", point}}}
    //        };
    //        var mergedWhere = MergeWhere(nearWhere, new Dictionary<string, object> {
    //            { key, new Dictionary<string, object>{{"$maxDistance", maxDistance.Radians}}}
    //        });
    //        return CreateInstance( where: mergedWhere);
    //    }

    //    internal virtual S WhereRelatedTo(AVObject parent, string key)
    //    {
    //        return CreateInstance( where: new Dictionary<string, object> {
    //            {
    //                "$relatedTo",
    //                new Dictionary<string, object> {
    //                    { "object", parent},
    //                    { "key", key}
    //                }
    //            }
    //        });
    //    }

    //    #endregion

    //    /// <summary>
    //    /// Retrieves a list of AVObjects that satisfy this query from LeanCloud.
    //    /// </summary>
    //    /// <returns>The list of AVObjects that match this query.</returns>
    //    public virtual Task<IEnumerable<T>> FindAsync()
    //    {
    //        return FindAsync(CancellationToken.None);
    //    }

    //    /// <summary>
    //    /// Retrieves a list of AVObjects that satisfy this query from LeanCloud.
    //    /// </summary>
    //    /// <param name="cancellationToken">The cancellation token.</param>
    //    /// <returns>The list of AVObjects that match this query.</returns>
    //    public abstract Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken);


    //    /// <summary>
    //    /// Retrieves at most one AVObject that satisfies this query.
    //    /// </summary>
    //    /// <returns>A single AVObject that satisfies this query, or else null.</returns>
    //    public virtual Task<T> FirstOrDefaultAsync()
    //    {
    //        return FirstOrDefaultAsync(CancellationToken.None);
    //    }

    //    /// <summary>
    //    /// Retrieves at most one AVObject that satisfies this query.
    //    /// </summary>
    //    /// <param name="cancellationToken">The cancellation token.</param>
    //    /// <returns>A single AVObject that satisfies this query, or else null.</returns>
    //    public abstract Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken);

    //    /// <summary>
    //    /// Retrieves at most one AVObject that satisfies this query.
    //    /// </summary>
    //    /// <returns>A single AVObject that satisfies this query.</returns>
    //    /// <exception cref="AVException">If no results match the query.</exception>
    //    public virtual Task<T> FirstAsync()
    //    {
    //        return FirstAsync(CancellationToken.None);
    //    }

    //    /// <summary>
    //    /// Retrieves at most one AVObject that satisfies this query.
    //    /// </summary>
    //    /// <param name="cancellationToken">The cancellation token.</param>
    //    /// <returns>A single AVObject that satisfies this query.</returns>
    //    /// <exception cref="AVException">If no results match the query.</exception>
    //    public abstract Task<T> FirstAsync(CancellationToken cancellationToken);

    //    /// <summary>
    //    /// Counts the number of objects that match this query.
    //    /// </summary>
    //    /// <returns>The number of objects that match this query.</returns>
    //    public virtual Task<int> CountAsync()
    //    {
    //        return CountAsync(CancellationToken.None);
    //    }

    //    /// <summary>
    //    /// Counts the number of objects that match this query.
    //    /// </summary>
    //    /// <param name="cancellationToken">The cancellation token.</param>
    //    /// <returns>The number of objects that match this query.</returns>
    //    public abstract Task<int> CountAsync(CancellationToken cancellationToken);

    //    /// <summary>
    //    /// Constructs a AVObject whose id is already known by fetching data
    //    /// from the server.
    //    /// </summary>
    //    /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
    //    /// <returns>The AVObject for the given objectId.</returns>
    //    public virtual Task<T> GetAsync(string objectId)
    //    {
    //        return GetAsync(objectId, CancellationToken.None);
    //    }

    //    /// <summary>
    //    /// Constructs a AVObject whose id is already known by fetching data
    //    /// from the server.
    //    /// </summary>
    //    /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
    //    /// <param name="cancellationToken">The cancellation token.</param>
    //    /// <returns>The AVObject for the given objectId.</returns>
    //    public abstract Task<T> GetAsync(string objectId, CancellationToken cancellationToken);

    //    internal object GetConstraint(string key)
    //    {
    //        return where == null ? null : where.GetOrDefault(key, null);
    //    }

    //    /// <summary>
    //    /// 构建查询字符串
    //    /// </summary>
    //    /// <param name="includeClassName">是否包含 ClassName </param>
    //    /// <returns></returns>
    //    public IDictionary<string, object> BuildParameters(bool includeClassName = false)
    //    {
    //        Dictionary<string, object> result = new Dictionary<string, object>();
    //        if (where != null)
    //        {
    //            result["where"] = PointerOrLocalIdEncoder.Instance.Encode(where);
    //        }
    //        if (orderBy != null)
    //        {
    //            result["order"] = string.Join(",", orderBy.ToArray());
    //        }
    //        if (skip != null)
    //        {
    //            result["skip"] = skip.Value;
    //        }
    //        if (limit != null)
    //        {
    //            result["limit"] = limit.Value;
    //        }
    //        if (includes != null)
    //        {
    //            result["include"] = string.Join(",", includes.ToArray());
    //        }
    //        if (selectedKeys != null)
    //        {
    //            result["keys"] = string.Join(",", selectedKeys.ToArray());
    //        }
    //        if (includeClassName)
    //        {
    //            result["className"] = className;
    //        }
    //        if (redirectClassNameForKey != null)
    //        {
    //            result["redirectClassNameForKey"] = redirectClassNameForKey;
    //        }
    //        return result;
    //    }

    //    private string RegexQuote(string input)
    //    {
    //        return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
    //    }

    //    private string GetRegexOptions(Regex regex, string modifiers)
    //    {
    //        string result = modifiers ?? "";
    //        if (regex.Options.HasFlag(RegexOptions.IgnoreCase) && !modifiers.Contains("i"))
    //        {
    //            result += "i";
    //        }
    //        if (regex.Options.HasFlag(RegexOptions.Multiline) && !modifiers.Contains("m"))
    //        {
    //            result += "m";
    //        }
    //        return result;
    //    }

    //    private IDictionary<string, object> EncodeRegex(Regex regex, string modifiers)
    //    {
    //        var options = GetRegexOptions(regex, modifiers);
    //        var dict = new Dictionary<string, object>();
    //        dict["$regex"] = regex.ToString();
    //        if (!string.IsNullOrEmpty(options))
    //        {
    //            dict["$options"] = options;
    //        }
    //        return dict;
    //    }

    //    /// <summary>
    //    /// Serves as the default hash function.
    //    /// </summary>
    //    /// <returns>A hash code for the current object.</returns>
    //    public override int GetHashCode()
    //    {
    //        // TODO (richardross): Implement this.
    //        return 0;
    //    }
    //}
}
