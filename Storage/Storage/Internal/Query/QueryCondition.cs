using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LeanCloud.Storage.Internal {
    /// <summary>
    /// 查询条件类
    /// </summary>
    public class QueryCondition : IEquatable<QueryCondition> {
        public string Key {
            get; set;
        }

        public string Op {
            get; set;
        }

        public object Value {
            get; set;
        }

        public bool Equals(QueryCondition other) {
            return Key == other.Key && Op == other.Op;
        }

        public override bool Equals(object obj) {
            return obj is QueryCondition && Equals(obj as QueryCondition);
        }

        public override int GetHashCode() {
            return Key.GetHashCode() * 31 + Op.GetHashCode();
        }

        internal IDictionary<string, object> ToDictionary() {
            return new Dictionary<string, object> {
                {
                    Key, new Dictionary<string, object> {
                    { Op, PointerOrLocalIdEncoder.Instance.Encode(Value) }
                }
                }
            };
        }
    }
}
