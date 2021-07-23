using System.Collections.Generic;
using LC.Newtonsoft.Json;

namespace LeanCloud.Storage {
    public class LCSearchSortBuilder {
        private List<object> fields;

        public LCSearchSortBuilder() {
            fields = new List<object>();
        }

        public LCSearchSortBuilder OrderByAscending(string key, string mode = null, string missing = null) {
            return AddField(key, "asc", mode, missing);
        }

        public LCSearchSortBuilder OrderByDescending(string key, string mode = null, string missing = null) {
            return AddField(key, "desc", mode, missing);
        }

        public LCSearchSortBuilder WhereNear(string key, LCGeoPoint point,
            string order = null, string mode = null, string unit = null) {
            fields.Add(new Dictionary<string, object> {
                { "_geo_distance", new Dictionary<string, object> {
                    { key, new Dictionary<string, object> {
                        { "lat", point.Latitude },
                        { "lon", point.Longitude }
                    } },
                    { "order", order ?? "asc" },
                    { "mode", mode ?? "avg" },
                    { "unit", unit ?? "km" }
                } }
            });
            return this;
        }

        private LCSearchSortBuilder AddField(string key, string order = null, string mode = null, string missing = null) {
            fields.Add(new Dictionary<string, object> {
                { key, new Dictionary<string, object> {
                    { "order", order ?? "asc" },
                    { "mode", mode ?? "avg" },
                    { "missing", $"_{missing ?? "last"}" }
                } }
            });
            return this;
        }

        internal string Build() {
            return JsonConvert.SerializeObject(fields);
        }
    }
}
