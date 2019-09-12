using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using LeanCloud.Storage.Internal;

namespace LeanCloud {
    public class AVQuery2<T> where T : AVObject {
        public string ClassName {
            get; set;
        }

        internal string Op {
            get; set;
        }

        /// <summary>
        /// { key: { op: value } }
        /// </summary>
        public Dictionary<int, QueryCondition> Where {
            get; set;
        }

        internal ReadOnlyCollection<string> OrderBy {
            get; set;
        }

        internal ReadOnlyCollection<string> Includes {
            get; set;
        }

        internal ReadOnlyCollection<string> SelectedKeys {
            get; set;
        }

        internal string RedirectClassNameForKey {
            get; set;
        }

        internal int? Skip {
            get; set;
        }

        internal int? Limit {
            get; set;
        }

        public AVQuery2() {
            Op = "$and";
            Where = new Dictionary<int, QueryCondition>();
        }

        public static AVQuery2<T> And(IEnumerable<AVQuery2<T>> queries) {
            AVQuery2<T> combination = new AVQuery2<T>();
            if (queries != null) {
                foreach (AVQuery2<T> query in queries) {
                    query.BuildWhere();
                }
            }
            return combination;
        }

        #region where

        public AVQuery2<T> WhereEqualTo(string key, object value) {
            AddCondition(key, "$eq", value);
            return this;
        }

        #endregion

        IDictionary<string, object> BuildWhere() {
            if (Where.Count == 0) {
                return new Dictionary<string, object>();
            }
            if (Where.Count == 1) {
                return Where.Values.First().ToDictionary();
            }
            List<IDictionary<string, object>> conditions = new List<IDictionary<string, object>>();
            foreach (QueryCondition condition in Where.Values) {
                conditions.Add(condition.ToDictionary());
            }
            return new Dictionary<string, object> {
                { Op, conditions }
            };
        }

        void AddCondition(string key, string op, object value) {
            QueryCondition cond = new QueryCondition {
                Key = key,
                Op = op,
                Value = value
            };
            Where[cond.GetHashCode()] = cond;
        }
    }
}
