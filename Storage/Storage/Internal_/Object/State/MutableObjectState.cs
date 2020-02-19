using System;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    public class MutableObjectState : IObjectState {
        public string ClassName { get; set; }
        public string ObjectId { get; set; }
        public AVACL ACL { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }

        public IDictionary<string, object> ServerData {
            get; set;
        } = new Dictionary<string, object>();

        public object this[string key] {
            get {
                return ServerData[key];
            }
        }

        public bool ContainsKey(string key) {
            return ServerData.ContainsKey(key);
        }

        public void Apply(IDictionary<string, IAVFieldOperation> operationSet) {
            // Apply operationSet
            foreach (var pair in operationSet) {
                ServerData.TryGetValue(pair.Key, out object oldValue);
                var newValue = pair.Value.Apply(oldValue, pair.Key);
                if (newValue != AVDeleteOperation.DeleteToken) {
                    ServerData[pair.Key] = newValue;
                } else {
                    ServerData.Remove(pair.Key);
                }
            }
        }

        public void Apply(IObjectState other) {
            if (other.ObjectId != null) {
                ObjectId = other.ObjectId;
            }
            if (other.ACL != null) {
                ACL = other.ACL;
            }
            if (other.UpdatedAt != null) {
                UpdatedAt = other.UpdatedAt;
            }
            if (other.CreatedAt != null) {
                CreatedAt = other.CreatedAt;
            }

            foreach (var pair in other) {
                ServerData[pair.Key] = pair.Value;
            }
        }

        public IObjectState MutatedClone(Action<MutableObjectState> func) {
            var clone = MutableClone();
            func(clone);
            return clone;
        }

        protected virtual MutableObjectState MutableClone() {
            return new MutableObjectState {
                ClassName = ClassName,
                ObjectId = ObjectId,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                ServerData = new Dictionary<string, object>(ServerData)
            };
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            return ServerData.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
        }
    }
}
