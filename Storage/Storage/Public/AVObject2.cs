using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;

namespace Storage.Public {
    public class AVObject2 : IDictionary<string, object> {
        ConcurrentDictionary<string, object> data;

        public object this[string key] { get => ((IDictionary<string, object>)data)[key]; set => ((IDictionary<string, object>)data)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, object>)data).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>)data).Values;

        public int Count => ((IDictionary<string, object>)data).Count;

        public bool IsReadOnly => ((IDictionary<string, object>)data).IsReadOnly;

        public void Add(string key, object value) {
            ((IDictionary<string, object>)data).Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item) {
            ((IDictionary<string, object>)data).Add(item);
        }

        public void Clear() {
            ((IDictionary<string, object>)data).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item) {
            return ((IDictionary<string, object>)data).Contains(item);
        }

        public bool ContainsKey(string key) {
            return ((IDictionary<string, object>)data).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
            ((IDictionary<string, object>)data).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return ((IDictionary<string, object>)data).GetEnumerator();
        }

        public bool Remove(string key) {
            return ((IDictionary<string, object>)data).Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item) {
            return ((IDictionary<string, object>)data).Remove(item);
        }

        public bool TryGetValue(string key, out object value) {
            return ((IDictionary<string, object>)data).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IDictionary<string, object>)data).GetEnumerator();
        }
    }
}
