using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LeanCloud.Play {
    /// <summary>
    /// 数组类结构，实现 IList 接口
    /// </summary>
    public class PlayArray : IList {
        internal IList Data {
            get; private set;
        }

        /// <summary>
        /// 是否固定长度
        /// </summary>
        /// <value><c>true</c> if is fixed size; otherwise, <c>false</c>.</value>
        public bool IsFixedSize => Data.IsFixedSize;

        /// <summary>
        /// 是否只读
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly => Data.IsReadOnly;

        /// <summary>
        /// 获取长度
        /// </summary>
        /// <value>The count.</value>
        public int Count => Data.Count;

        /// <summary>
        /// 是否同步访问
        /// </summary>
        /// <value><c>true</c> if is synchronized; otherwise, <c>false</c>.</value>
        public bool IsSynchronized => Data.IsSynchronized;

        public object SyncRoot => Data.SyncRoot;

        /// <summary>
        /// 访问元素
        /// </summary>
        /// <param name="index">索引</param>
        public object this[int index] { get => Data[index]; set => Data[index] = value; }

        /// <summary>
        /// 增加元素
        /// </summary>
        /// <returns>The add.</returns>
        /// <param name="value">元素</param>
        public int Add(object value) {
            return Data.Add(value);
        }

        /// <summary>
        /// 清空所有元素
        /// </summary>
        public void Clear() {
            Data.Clear();
        }

        /// <summary>
        /// 是否包含元素
        /// </summary>
        /// <returns>The contains.</returns>
        /// <param name="value">元素</param>
        public bool Contains(object value) {
            return Data.Contains(value);
        }

        /// <summary>
        /// 元素索引
        /// </summary>
        /// <returns>The of.</returns>
        /// <param name="value">元素</param>
        public int IndexOf(object value) {
            return Data.IndexOf(value);
        }

        /// <summary>
        /// 在索引处插入元素
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="value">元素</param>
        public void Insert(int index, object value) {
            Data.Insert(index, value);
        }

        /// <summary>
        /// 移除元素
        /// </summary>
        /// <param name="value">Value.</param>
        public void Remove(object value) {
            Data.Remove(value);
        }

        /// <summary>
        /// 移除索引处的元素
        /// </summary>
        /// <param name="index">Index.</param>
        public void RemoveAt(int index) {
            Data.RemoveAt(index);
        }

        /// <summary>
        /// 拷贝元素到数组
        /// </summary>
        /// <param name="array">目标数组</param>
        /// <param name="index">数组起始索引</param>
        public void CopyTo(Array array, int index) {
            Data.CopyTo(array, index);
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator GetEnumerator() {
            return Data.GetEnumerator();
        }

        // 扩展方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="capacity">容量</param>
        public PlayArray(int capacity) {
            Data = new List<object>(capacity);
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public PlayArray() : this(0) {

        }

        /// <summary>
        /// 构造方法，
        /// </summary>
        /// <param name="data">数组对象</param>
        public PlayArray(IList data) : this() {
            if (data != null) {
                foreach (var d in data) {
                    Data.Add(d);
                }
            }
        }

        /// <summary>
        /// 获取元素 bool 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public bool GetBool(int index) {
            return bool.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 byte 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public byte GetByte(int index) {
            return byte.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 short 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public short GetShort(int index) {
            return short.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 int 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public int GetInt(int index) {
            return int.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 long 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public long GetLong(int index) {
            return long.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 float 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public float GetFloat(int index) {
            return float.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 double 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public double GetDouble(int index) {
            return double.Parse(Data[index].ToString());
        }

        /// <summary>
        /// 获取元素 string 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public string GetString(int index) {
            return Data[index].ToString();
        }

        /// <summary>
        /// 获取元素 byte[] 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public byte[] GetBytes(int index) {
            return Data[index] as byte[];
        }

        /// <summary>
        /// 获取元素 PlayObject 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public PlayObject GetPlayObject(int index) {
            return Data[index] as PlayObject;
        }

        /// <summary>
        /// 获取元素 PlayArray 值
        /// </summary>
        /// <returns><c>true</c>, if bool was gotten, <c>false</c> otherwise.</returns>
        /// <param name="index">索引</param>
        public PlayArray GetPlayArray(int index) {
            return Data[index] as PlayArray;
        }

        /// <summary>
        /// 获取元素的具体类型值
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="index">Index.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>(int index) {
            return (T)Data[index];
        }

        /// <summary>
        /// 转换至 T 类型链表
        /// </summary>
        /// <returns>The list.</returns>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public List<T> ToList<T>() {
            return Data.Cast<T>().ToList();
        }

        /// <summary>
        /// 属性是否为空
        /// </summary>
        /// <returns><c>true</c>, if null was ised, <c>false</c> otherwise.</returns>
        /// <param name="index">Index.</param>
        public bool IsNull(int index) {
            return Data[index] == null;
        }
    }
}
