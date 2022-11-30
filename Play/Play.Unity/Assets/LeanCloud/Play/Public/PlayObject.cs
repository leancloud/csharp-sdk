using System.Collections;
using System.Collections.Generic;

namespace LeanCloud.Play {
    /// <summary>
    /// 字典类结构，实现 IDictionary 接口
    /// </summary>
    public class PlayObject : IDictionary<object, object> {
        internal Dictionary<object, object> Data {
            get; private set;
        }

        /// <summary>
        /// 属性名称列表
        /// </summary>
        /// <value>The keys.</value>
        public ICollection<object> Keys => ((IDictionary<object, object>)Data).Keys;

        /// <summary>
        /// 属性值列表
        /// </summary>
        /// <value>The values.</value>
        public ICollection<object> Values => ((IDictionary<object, object>)Data).Values;

        /// <summary>
        /// 属性数量
        /// </summary>
        /// <value>The count.</value>
        public int Count => ((IDictionary<object, object>)Data).Count;

        /// <summary>
        /// 是否只读
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly => ((IDictionary<object, object>)Data).IsReadOnly;

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <param name="key">属性名称</param>
        public object this[object key] { get => ((IDictionary<object, object>)Data)[key]; set => ((IDictionary<object, object>)Data)[key] = value; }

        /// <summary>
        /// 增加属性
        /// </summary>
        /// <param name="key">属性名称</param>
        /// <param name="value">属性值</param>
        public void Add(object key, object value) {
            ((IDictionary<object, object>)Data).Add(key, value);
        }

        /// <summary>
        /// 是否包含属性
        /// </summary>
        /// <returns><c>true</c>, if key was containsed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        public bool ContainsKey(object key) {
            return ((IDictionary<object, object>)Data).ContainsKey(key);
        }

        /// <summary>
        /// 移除属性
        /// </summary>
        /// <returns>The remove.</returns>
        /// <param name="key">属性名称</param>
        public bool Remove(object key) {
            return ((IDictionary<object, object>)Data).Remove(key);
        }

        /// <summary>
        /// 尝试获取属性值
        /// </summary>
        /// <returns><c>true</c>, if get value was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="value">属性值</param>
        public bool TryGetValue(object key, out object value) {
            return ((IDictionary<object, object>)Data).TryGetValue(key, out value);
        }

        /// <summary>
        /// 增加属性
        /// </summary>
        /// <param name="item">Item.</param>
        public void Add(KeyValuePair<object, object> item) {
            ((IDictionary<object, object>)Data).Add(item);
        }

        /// <summary>
        /// 清空属性
        /// </summary>
        public void Clear() {
            ((IDictionary<object, object>)Data).Clear();
        }

        /// <summary>
        /// 是否包含属性
        /// </summary>
        /// <returns>The contains.</returns>
        /// <param name="item">属性</param>
        public bool Contains(KeyValuePair<object, object> item) {
            return ((IDictionary<object, object>)Data).Contains(item);
        }

        /// <summary>
        /// 拷贝属性到数组
        /// </summary>
        /// <param name="array">目标属性数组</param>
        /// <param name="arrayIndex">数组索引</param>
        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) {
            ((IDictionary<object, object>)Data).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 移除属性
        /// </summary>
        /// <returns>The remove.</returns>
        /// <param name="item">属性</param>
        public bool Remove(KeyValuePair<object, object> item) {
            return ((IDictionary<object, object>)Data).Remove(item);
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<object, object>> GetEnumerator() {
            return ((IDictionary<object, object>)Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IDictionary<object, object>)Data).GetEnumerator();
        }

        // 扩展接口
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="capacity">属性数量</param>
        public PlayObject(int capacity) {
            Data = new Dictionary<object, object>(capacity);
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public PlayObject() : this(0) {

        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="dictionary">字典对象</param>
        public PlayObject(IDictionary dictionary) : this() {
            if (dictionary != null) {
                foreach (DictionaryEntry entry in dictionary) {
                    Data.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        /// <value><c>true</c> if is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty {
            get {
                return Data == null || Data.Count == 0;
            }
        }

        /// <summary>
        /// 尝试获取属性的 bool 值
        /// </summary>
        /// <returns><c>true</c>, if get bool was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetBool(object key, out bool val) {
            if (Data.TryGetValue(key, out var valObj) &&
                bool.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = false;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 byte 值
        /// </summary>
        /// <returns><c>true</c>, if get byte was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetByte(object key, out byte val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is byte &&
                byte.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 short 值
        /// </summary>
        /// <returns><c>true</c>, if get short was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetShort(object key, out short val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is short &&
                short.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 int 值
        /// </summary>
        /// <returns><c>true</c>, if get int was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetInt(object key, out int val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is int &&
                int.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 long 值
        /// </summary>
        /// <returns><c>true</c>, if get long was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetLong(object key, out long val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is long &&
                long.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 float 值
        /// </summary>
        /// <returns><c>true</c>, if get float was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetFloat(object key, out float val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is float &&
                float.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0f;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 double 值
        /// </summary>
        /// <returns><c>true</c>, if get double was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetDouble(object key, out double val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is double &&
                double.TryParse(valObj.ToString(), out val)) {
                return true;
            }
            val = 0;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 string 值
        /// </summary>
        /// <returns><c>true</c>, if get string was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetString(object key, out string val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is string) { 
                val = valObj.ToString();
                return true;
            }
            val = null;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 byte[] 值
        /// </summary>
        /// <returns><c>true</c>, if get bytes was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetBytes(object key, out byte[] val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is byte[]) {
                val = valObj as byte[];
                return true;
            }
            val = null;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 PlayObject 值
        /// </summary>
        /// <returns><c>true</c>, if get play object was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetPlayObject(object key, out PlayObject val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is PlayObject) {
                val = valObj as PlayObject;
                return true;
            }
            val = null;
            return false;
        }

        /// <summary>
        /// 尝试获取属性的 PlayArray 值
        /// </summary>
        /// <returns><c>true</c>, if get play array was tryed, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        /// <param name="val">返回属性值</param>
        public bool TryGetPlayArray(object key, out PlayArray val) {
            if (Data.TryGetValue(key, out var valObj) && valObj is PlayArray) {
                val = valObj as PlayArray;
                return true;
            }
            val = null;
            return false;
        }

        /// <summary>
        /// 获取属性 bool 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="key">属性名称</param>
        public bool GetBool(object key) {
            TryGetBool(key, out bool val);
            return val;
        }

        /// <summary>
        /// 获取属性 byte 值
        /// </summary>
        /// <returns>The byte.</returns>
        /// <param name="key">属性名称</param>
        public byte GetByte(object key) {
            TryGetByte(key, out byte val);
            return val;
        }

        /// <summary>
        /// 获取属性 short 值
        /// </summary>
        /// <returns>The short.</returns>
        /// <param name="key">属性名称</param>
        public short GetShort(object key) {
            TryGetShort(key, out short val);
            return val;
        }

        /// <summary>
        /// 获取属性 int 值
        /// </summary>
        /// <returns>The int.</returns>
        /// <param name="key">属性名称</param>
        public int GetInt(object key) {
            TryGetInt(key, out int val);
            return val;
        }

        /// <summary>
        /// 获取属性 long 值
        /// </summary>
        /// <returns>The long.</returns>
        /// <param name="key">属性名称</param>
        public long GetLong(object key) {
            TryGetLong(key, out long val);
            return val;
        }

        /// <summary>
        /// 获取属性 float 值
        /// </summary>
        /// <returns>The float.</returns>
        /// <param name="key">属性名称</param>
        public float GetFloat(object key) {
            TryGetFloat(key, out float val);
            return val;
        }

        /// <summary>
        /// 获取属性 double 值
        /// </summary>
        /// <returns>The double.</returns>
        /// <param name="key">属性名称</param>
        public double GetDouble(object key) {
            TryGetDouble(key, out double val);
            return val;
        }

        /// <summary>
        /// 获取属性 string 值
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="key">属性名称</param>
        public string GetString(object key) {
            TryGetString(key, out string val);
            return val;
        }

        /// <summary>
        /// 获取属性 byte[] 值
        /// </summary>
        /// <returns>The bytes.</returns>
        /// <param name="key">属性名称</param>
        public byte[] GetBytes(object key) {
            TryGetBytes(key, out byte[] val);
            return val;
        }

        /// <summary>
        /// 获取属性 PlayObject 值
        /// </summary>
        /// <returns>The play object.</returns>
        /// <param name="key">属性名称</param>
        public PlayObject GetPlayObject(object key) {
            TryGetPlayObject(key, out PlayObject val);
            return val;
        }

        /// <summary>
        /// 获取属性 PlayArray 值
        /// </summary>
        /// <returns>The play array.</returns>
        /// <param name="key">属性名称</param>
        public PlayArray GetPlayArray(object key) {
            TryGetPlayArray(key, out PlayArray val);
            return val;
        }

        /// <summary>
        /// 获取属性 T 值
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="key">Key.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>(object key) {
            return (T) Data[key];
        }

        /// <summary>
        /// 属性是否为空
        /// </summary>
        /// <returns><c>true</c>, if null was ised, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool IsNull(object key) {
            return Data[key] == null;
        }
    }
}
