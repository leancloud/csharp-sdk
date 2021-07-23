using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Object;

namespace LeanCloud.Storage {
    public class LCSearchQuery<T> where T : LCObject {
        private string className;
        private string queryString;
        private IEnumerable<string> highlights;
        private IEnumerable<string> includeKeys;
        private int limit;
        private int skip;
        private string sid;
        private List<string> orders;
        private LCSearchSortBuilder sortBuilder;

        public LCSearchQuery(string className) {
            this.className = className;
            limit = 100;
            skip = 0;
        }

        public LCSearchQuery<T> QueryString(string q) {
            queryString = q;
            return this;
        }

        public LCSearchQuery<T> Highlights(IEnumerable<string> highlights) {
            this.highlights = highlights;
            return this;
        }

        public LCSearchQuery<T> Include(IEnumerable<string> keys) {
            includeKeys = keys;
            return this;
        }

        public LCSearchQuery<T> Limit(int amount) {
            limit = amount;
            return this;
        }

        public LCSearchQuery<T> Skip(int amount) {
            skip = amount;
            return this;
        }

        public LCSearchQuery<T> Sid(string sid) {
            this.sid = sid;
            return this;
        }

        public LCSearchQuery<T> OrderByAscending(string key) {
            orders = new List<string>();
            orders.Add(key);
            return this;
        }

        public LCSearchQuery<T> OrderByDescending(string key) {
            return OrderByAscending($"-{key}");
        }

        public LCSearchQuery<T> AddAscendingOrder(string key) {
            if (orders == null) {
                orders = new List<string>();
            }
            orders.Add(key);
            return this;
        }

        public LCSearchQuery<T> AddDescendingOrder(string key) {
            return AddAscendingOrder($"-{key}");
        }

        public LCSearchQuery<T> SortBy(LCSearchSortBuilder sortBuilder) {
            this.sortBuilder = sortBuilder;
            return this;
        }

        public async Task<LCSearchResponse<T>> Find() {
            string path = "search/select";
            Dictionary<string, object> queryParams = new Dictionary<string, object> {
                { "clazz", className },
                { "limit", limit },
                { "skip", skip }
            };
            if (queryString != null) {
                queryParams["q"] = queryString;
            }
            if (highlights != null && highlights.Count() > 0) {
                queryParams["highlights"] = string.Join(",", highlights);
            }
            if (includeKeys != null && includeKeys.Count() > 0) {
                queryParams["include"] = string.Join(",", includeKeys);
            }
            if (!string.IsNullOrEmpty(sid)) {
                queryParams["sid"] = sid;
            }
            if (orders != null && orders.Count() > 0) {
                queryParams["order"] = string.Join(",", orders);
            }
            if (sortBuilder != null) {
                queryParams["sort"] = sortBuilder.Build();
            }

            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>(path, queryParams: queryParams);
            LCSearchResponse<T> ret = new LCSearchResponse<T>();
            ret.Hits = (int)response["hits"];
            ret.Sid = (string)response["sid"];
            List<object> results = response["results"] as List<object>;
            List<T> list = new List<T>();
            foreach (object data in results) {
                LCObjectData objectData = LCObjectData.Decode(data as Dictionary<string, object>);
                T obj = LCObject.Create(className) as T;
                obj.Merge(objectData);
                list.Add(obj);
            }
            ret.Results = list.AsReadOnly();
            return ret;
        }
    }
}
