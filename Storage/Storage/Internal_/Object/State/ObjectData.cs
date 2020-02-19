using System;
using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Storage.Internal {
    internal class ObjectData : IEnumerable<KeyValuePair<string, object>> {
        internal string ClassName {
            get; set;
        }

        internal string ObjectId {
            get; set;
        }

        internal AVACL ACL {
            get; set;
        }

        internal DateTime? CreatedAt {
            get; set;
        }

        internal DateTime? UpdatedAt {
            get; set;
        }

        internal Dictionary<string, object> CustomProperties {
            get; set;
        } = new Dictionary<string, object>();

        internal object this[string key] {
            get {
                return CustomProperties[key];
            }
        }

        internal bool ContainsKey(string key) {
            return CustomProperties.ContainsKey(key);
        }

        internal void Apply(Dictionary<string, IAVFieldOperation> operations) {
            foreach (KeyValuePair<string, IAVFieldOperation> entry in operations) {
                string propKey = entry.Key;
                object propVal = entry.Value;
                if (!CustomProperties.TryGetValue(propKey, out object oldVal)) {
                    continue;
                }
                object newVal = entry.Value.Apply(oldVal, propKey);
                if (newVal == AVDeleteOperation.DeleteToken) {
                    CustomProperties.Remove(propKey);
                } else {
                    CustomProperties[propKey] = newVal;
                }
            }
        }

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, object>>)CustomProperties).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, object>>)CustomProperties).GetEnumerator();
        }

        #endregion
    }
}
