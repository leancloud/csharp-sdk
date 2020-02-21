using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Operation;

namespace LeanCloud.Storage {
    /// <summary>
    /// 对象类
    /// </summary>
    public class LCObject {
        /// <summary>
        /// 最近一次与服务端同步的数据
        /// </summary>
        LCObjectData data;

        /// <summary>
        /// 预算数据
        /// </summary>
        Dictionary<string, object> estimatedData;

        /// <summary>
        /// 操作字典
        /// </summary>
        Dictionary<string, ILCOperation> operationDict;

        static readonly Dictionary<Type, LCSubclassInfo> subclassTypeDict = new Dictionary<Type, LCSubclassInfo>();
        static readonly Dictionary<string, LCSubclassInfo> subclassNameDict = new Dictionary<string, LCSubclassInfo>();

        public string ClassName {
            get {
                return data.ClassName;
            }
        }

        public string ObjectId {
            get {
                return data.ObjectId;
            }
        }

        public DateTime CreatedAt {
            get {
                return data.CreatedAt;
            }
        }

        public DateTime UpdatedAt {
            get {
                return data.UpdatedAt;
            }
        }

        public LCACL ACL {
            get {
                return this["ACL"] as LCACL ;
            }
        }

        bool isNew;

        public LCObject(string className) {
            if (string.IsNullOrEmpty(className)) {
                throw new ArgumentNullException(nameof(className));
            }
            data = new LCObjectData();
            estimatedData = new Dictionary<string, object>();
            operationDict = new Dictionary<string, ILCOperation>();

            data.ClassName = className;
            isNew = true;
        }

        public static LCObject CreateWithoutData(string className, string objectId) {
            if (string.IsNullOrEmpty(objectId)) {
                throw new ArgumentNullException(nameof(objectId));
            }
            LCObject obj = new LCObject(className);
            obj.data.ObjectId = objectId;
            obj.isNew = false;
            return null;
        }

        internal static LCObject Create(string className) {
            if (subclassNameDict.TryGetValue(className, out LCSubclassInfo subclassInfo)) {
                return subclassInfo.Constructor.Invoke();
            }
            return new LCObject(className);
        }

        internal static LCObject Create(Type type) {
            if (subclassTypeDict.TryGetValue(type, out LCSubclassInfo subclassInfo)) {
                return subclassInfo.Constructor.Invoke();
            }
            return null;
        }

        public object this[string key] {
            get {
                object value = estimatedData[key];
                if (value is LCRelation<LCObject> relation) {
                    relation.Key = key;
                    relation.Parent = this;
                }
                return value;
            }
            set {
                if (string.IsNullOrEmpty(key)) {
                    throw new ArgumentNullException(nameof(key));
                }
                if (key.StartsWith("_")) {
                    throw new ArgumentException("key should not start with '_'");
                }
                if (key == "objectId" || key == "createdAt" || key == "updatedAt") {
                    throw new ArgumentException($"{key} is reserved by LeanCloud");
                }
                LCSetOperation setOp = new LCSetOperation(value);
                ApplyOperation(key, setOp);
            }
        }

        public void Unset(string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            LCDeleteOperation deleteOp = new LCDeleteOperation();
            ApplyOperation(key, deleteOp);
        }

        public static void RegisterSubclass(string className, Type type, Func<LCObject> constructor) {
            LCSubclassInfo subclassInfo = new LCSubclassInfo(className, type, constructor);
            subclassNameDict[className] = subclassInfo;
            subclassTypeDict[type] = subclassInfo;
        }

        void ApplyOperation(string key, ILCOperation op) {
            if (operationDict.TryGetValue(key, out ILCOperation previousOp)) {
                operationDict[key] = op.MergeWithPrevious(previousOp);
            } else {
                operationDict[key] = op;
            }
            if (op is LCDeleteOperation) {
                estimatedData.Remove(key);
            } else {
                object oldValue = estimatedData[key];
                estimatedData[key] = op.Apply(oldValue, key);
            }
        }

        internal void Merge(LCObjectData objectData) {
            data.ClassName = objectData.ClassName ?? data.ClassName;
            data.ObjectId = objectData.ObjectId ?? data.ObjectId;
            data.CreatedAt = objectData.CreatedAt != null ? objectData.CreatedAt : data.CreatedAt;
            data.UpdatedAt = objectData.UpdatedAt != null ? objectData.UpdatedAt : data.UpdatedAt;
            // 先将本地的预估数据直接替换
            data.CustomPropertyDict = estimatedData;
            // 再将服务端的数据覆盖
            foreach (KeyValuePair<string, object> kv in objectData.CustomPropertyDict) {
                string key = kv.Key;
                object value = kv.Value;
                data.CustomPropertyDict[key] = value;
            }

            // 最后重新生成预估数据，用于后续访问和操作
            RebuildEstimatedData();
            // 清空操作
            operationDict.Clear();
            isNew = false;
        }

        void RebuildEstimatedData() {
            estimatedData = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in data.CustomPropertyDict) {
                string key = kv.Key;
                object value = kv.Value;
                if (value is IList list) {
                    estimatedData[key] = new List<object>(list.Cast<object>());
                } else if (value is IDictionary dict) {
                    Dictionary<string, object> d = new Dictionary<string, object>();
                    foreach (DictionaryEntry entry in dict) {
                        string k = entry.Key.ToString();
                        object v = entry.Value;
                        d[k] = v;
                    }
                    estimatedData[key] = d;
                } else {
                    estimatedData[key] = value;
                }
            }
        }
    }
}
