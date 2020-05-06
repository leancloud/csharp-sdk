using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Operation;
using LeanCloud.Storage.Internal.Codec;

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
        internal Dictionary<string, object> estimatedData;

        /// <summary>
        /// 操作字典
        /// </summary>
        internal Dictionary<string, ILCOperation> operationDict;

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
            } set {
                this["ACL"] = value;
            }
        }

        bool isNew;

        bool IsDirty {
            get {
                return isNew || estimatedData.Count > 0;
            }
        }

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
            LCObject obj = Create(className);
            obj.data.ObjectId = objectId;
            obj.isNew = false;
            return obj;
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
                if (estimatedData.TryGetValue(key, out object value)) {
                    if (value is LCRelation<LCObject> relation) {
                        relation.Key = key;
                        relation.Parent = this;
                    }
                    return value;
                }
                return null;
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

        /// <summary>
        /// 删除字段
        /// </summary>
        /// <param name="key"></param>
        public void Unset(string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            LCDeleteOperation deleteOp = new LCDeleteOperation();
            ApplyOperation(key, deleteOp);
        }

        /// <summary>
        /// 增加关联
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddRelation(string key, LCObject value) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            LCAddRelationOperation op = new LCAddRelationOperation(new List<LCObject> { value });
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 删除关联
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void RemoveRelation(string key, LCObject value) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            LCRemoveRelationOperation op = new LCRemoveRelationOperation(value);
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 增加数字属性值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Increment(string key, object value) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            LCNumberOperation op = new LCNumberOperation(value);
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 在数组属性中增加一个元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, object value) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            LCAddOperation op = new LCAddOperation(new List<object> { value });
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 在数组属性中增加一组元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public void AddAll(string key, IEnumerable values) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            LCAddOperation op = new LCAddOperation(new List<object>(values.Cast<object>()));
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 在数组属性中增加一个唯一元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddUnique(string key, object value) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            LCAddUniqueOperation op = new LCAddUniqueOperation(new List<object> { value });
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 在数组属性中增加一组唯一元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddAllUnique(string key, IEnumerable values) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            LCAddUniqueOperation op = new LCAddUniqueOperation(new List<object>(values.Cast<object>()));
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 移除某个元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Remove(string key, object value) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            LCRemoveOperation op = new LCRemoveOperation(new List<object> { value });
            ApplyOperation(key, op);
        }

        /// <summary>
        /// 移除一组元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public void RemoveAll(string key, IEnumerable values) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }
            LCRemoveOperation op = new LCRemoveOperation(new List<object>(values.Cast<object>()));
            ApplyOperation(key, op);
        }

        static async Task SaveBatches(Stack<LCBatch> batches) {
            while (batches.Count > 0) {
                LCBatch batch = batches.Pop();
                List<LCObject> dirtyObjects = batch.objects.Where(item => item.IsDirty)
                                                            .ToList();

                List<Dictionary<string, object>> requestList = dirtyObjects.Select(item => {
                    string path = item.ObjectId == null ?
                                $"/1.1/classes/{item.ClassName}" :
                                $"/1.1/classes/{item.ClassName}/{item.ClassName}";
                    string method = item.ObjectId == null ? "POST" : "PUT";
                    Dictionary<string, object> body = LCEncoder.Encode(item.operationDict) as Dictionary<string, object>;
                    return new Dictionary<string, object> {
                        { "path", path },
                        { "method", method },
                        { "body", body }
                    };
                }).ToList();

                Dictionary<string, object> data = new Dictionary<string, object> {
                    { "requests", LCEncoder.Encode(requestList) }
                };

                List<Dictionary<string, object>> results = await LCApplication.HttpClient.Post<List<Dictionary<string, object>>>("batch", data: data);
                List<LCObjectData> resultList = results.Select(item => {
                    if (item.TryGetValue("error", out object error)) {
                        Dictionary<string, object> err = error as Dictionary<string, object>;
                        int code = (int)err["code"];
                        string message = (string)err["error"];
                        throw new LCException(code, message as string);
                    }
                    return LCObjectData.Decode(item["success"] as IDictionary);
                }).ToList();

                for (int i = 0; i < dirtyObjects.Count; i++) {
                    LCObject obj = dirtyObjects[i];
                    LCObjectData objData = resultList[i];
                    obj.Merge(objData);
                }
            }
        }

        public async Task<LCObject> Save(bool fetchWhenSave = false, LCQuery<LCObject> query = null) {
            if (LCBatch.HasCircleReference(this, new HashSet<LCObject>())) {
                throw new ArgumentException("Found a circle dependency when save.");
            }

            Stack<LCBatch> batches = LCBatch.BatchObjects(new List<LCObject> { this }, false);
            if (batches.Count > 0) {
                await SaveBatches(batches);
            }

            string path = ObjectId == null ? $"classes/{ClassName}" : $"classes/{ClassName}/{ObjectId}";
            Dictionary<string, object> queryParams = new Dictionary<string, object>();
            if (fetchWhenSave) {
                queryParams["fetchWhenSave"] = true;
            }
            if (query != null) {
                queryParams["where"] = query.BuildWhere();
            }
            Dictionary<string, object> response = ObjectId == null ?
                await LCApplication.HttpClient.Post<Dictionary<string, object>>(path, data: LCEncoder.Encode(operationDict) as Dictionary<string, object>, queryParams: queryParams) :
                await LCApplication.HttpClient.Put<Dictionary<string, object>>(path, data: LCEncoder.Encode(operationDict) as Dictionary<string, object>, queryParams: queryParams);
            LCObjectData data = LCObjectData.Decode(response);
            Merge(data);
            return this;
        }

        public static async Task<List<LCObject>> SaveAll(List<LCObject> objectList) {
            if (objectList == null) {
                throw new ArgumentNullException(nameof(objectList));
            }
            foreach (LCObject obj in objectList) {
                if (LCBatch.HasCircleReference(obj, new HashSet<LCObject>())) {
                    throw new ArgumentException("Found a circle dependency when save.");
                }
            }
            Stack<LCBatch> batches = LCBatch.BatchObjects(objectList, true);
            await SaveBatches(batches);
            return objectList;
        }

        public async Task Delete() {
            if (ObjectId == null) {
                return;
            }
            string path = $"classes/{ClassName}/{ObjectId}";
            await LCApplication.HttpClient.Delete(path);
        }

        public static async Task DeleteAll(List<LCObject> objectList) {
            if (objectList == null || objectList.Count == 0) {
                throw new ArgumentNullException(nameof(objectList));
            }
            IEnumerable<LCObject> objects = objectList.Where(item => item.ObjectId != null);
            HashSet<LCObject> objectSet = new HashSet<LCObject>(objects);
            List<Dictionary<string, object>> requestList = objectSet.Select(item => {
                string path = $"/{LCApplication.APIVersion}/classes/{item.ClassName}/{item.ObjectId}";
                return new Dictionary<string, object> {
                    { "path", path },
                    { "method", "DELETE" }
                };
            }).ToList();
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "requests", LCEncoder.Encode(requestList) }
            };
            await LCApplication.HttpClient.Post<List<object>>("batch", data: data);
        }

        public async Task<LCObject> Fetch(IEnumerable<string> keys = null, IEnumerable<string> includes = null) {
            Dictionary<string, object> queryParams = new Dictionary<string, object>();
            if (keys != null) {
                queryParams["keys"] = string.Join(",", keys);
            }
            if (includes != null) {
                queryParams["include"] = string.Join(",", includes);
            }
            string path = $"classes/{ClassName}/{ObjectId}";
            Dictionary<string, object> response = await LCApplication.HttpClient.Get<Dictionary<string, object>>(path, queryParams: queryParams);
            LCObjectData objectData = LCObjectData.Decode(response);
            Merge(objectData);
            return this;
        }

        public static async Task<IEnumerable<LCObject>> FetchAll(IEnumerable<LCObject> objects) {
            if (objects == null || objects.Count() == 0) {
                throw new ArgumentNullException(nameof(objects));
            }

            IEnumerable<LCObject> uniqueObjects = objects.Where(item => item.ObjectId != null);
            List<Dictionary<string, object>> requestList = uniqueObjects.Select(item => {
                string path = $"/{LCApplication.APIVersion}/classes/{item.ClassName}/{item.ObjectId}";
                return new Dictionary<string, object> {
                    { "path", path },
                    { "method", "GET" }
                };
            }).ToList();

            Dictionary<string, object> data = new Dictionary<string, object> {
                { "requests", LCEncoder.Encode(requestList) }
            };
            List<Dictionary<string, object>> results = await LCApplication.HttpClient.Post<List<Dictionary<string, object>>>("batch",
                data: data);
            Dictionary<string, LCObjectData> dict = new Dictionary<string, LCObjectData>();
            foreach (Dictionary<string, object> item in results) {
                if (item.TryGetValue("error", out object error)) {
                    int code = (int)error;
                    string message = item["error"] as string;
                    throw new LCException(code, message);
                }
                Dictionary<string, object> d = item["success"] as Dictionary<string, object>;
                string objectId = d["objectId"] as string;
                dict[objectId] = LCObjectData.Decode(d);
            }
            foreach (LCObject obj in objects) {
                LCObjectData objData = dict[obj.ObjectId];
                obj.Merge(objData);
            }
            return objects;
        }

        public static void RegisterSubclass<T>(string className, Func<T> constructor) where T : LCObject {
            Type classType = typeof(T);
            LCSubclassInfo subclassInfo = new LCSubclassInfo(className, classType, constructor);
            subclassNameDict[className] = subclassInfo;
            subclassTypeDict[classType] = subclassInfo;
        }

        void ApplyOperation(string key, ILCOperation op) {
            if (op is LCDeleteOperation) {
                estimatedData.Remove(key);
            } else {
                if (estimatedData.TryGetValue(key, out object oldValue)) {
                    estimatedData[key] = op.Apply(oldValue, key);
                } else {
                    estimatedData[key] = op.Apply(null, key);
                }
            }
            if (operationDict.TryGetValue(key, out ILCOperation previousOp)) {
                operationDict[key] = op.MergeWithPrevious(previousOp);
            } else {
                operationDict[key] = op;
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
