using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Operation;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// LeanCloud Object
    /// </summary>
    public class LCObject {
        /// <summary>
        /// Last synced data.
        /// </summary>
        public LCObjectData Data {
            get;
        }

        /// <summary>
        /// Estimated data.
        /// </summary>
        internal Dictionary<string, object> estimatedData;

        /// <summary>
        /// Operations.
        /// </summary>
        internal Dictionary<string, ILCOperation> operationDict;

        static readonly Dictionary<Type, LCSubclassInfo> subclassTypeDict = new Dictionary<Type, LCSubclassInfo>();
        static readonly Dictionary<string, LCSubclassInfo> subclassNameDict = new Dictionary<string, LCSubclassInfo>();

        public string ClassName {
            get {
                return Data.ClassName;
            }
        }

        public string ObjectId {
            get {
                return Data.ObjectId;
            }
        }

        public DateTime CreatedAt {
            get {
                return Data.CreatedAt;
            }
        }

        public DateTime UpdatedAt {
            get {
                return Data.UpdatedAt;
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
            Data = new LCObjectData();
            estimatedData = new Dictionary<string, object>();
            operationDict = new Dictionary<string, ILCOperation>();

            Data.ClassName = className;
            isNew = true;
        }

        public static LCObject CreateWithoutData(string className, string objectId) {
            if (string.IsNullOrEmpty(objectId)) {
                throw new ArgumentNullException(nameof(objectId));
            }
            LCObject obj = Create(className);
            obj.Data.ObjectId = objectId;
            obj.isNew = false;
            return obj;
        }

        public static LCObject Create(string className) {
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
                if (key == "objectId" || key == "createdAt" || key == "updatedAt" ||
                    key == "className") {
                    throw new ArgumentException($"{key} is reserved by LeanCloud");
                }
                LCSetOperation setOp = new LCSetOperation(value);
                ApplyOperation(key, setOp);
            }
        }

        /// <summary>
        /// Removes the key.
        /// </summary>
        /// <param name="key"></param>
        public void Unset(string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            LCDeleteOperation deleteOp = new LCDeleteOperation();
            ApplyOperation(key, deleteOp);
        }


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
        /// Atomically increments the value of the given key with amount.
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
        /// Atomically adds value to the end of the array key.
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
        /// Atomically adds values to the end of the array key.
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
        /// Atomically adds value to the array key, only if not already present.
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
        /// Atomically adds values to the array key, only if not already present.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
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
        /// Atomically removes all value from the array key.
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
        /// Atomically removes all values from the array key.
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

        /// <summary>
        /// Serializes this LCObject to a JSON string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            Dictionary<string, object> originalData = LCObjectData.Encode(Data);
            Dictionary<string, object> currentData = estimatedData.Union(originalData.Where(kv => !estimatedData.ContainsKey(kv.Key)))
                .ToDictionary(k => k.Key, v => v.Value);
            return JsonConvert.SerializeObject(currentData);
        }

        /// <summary>
        /// Deserializes a JSON string to a LCObject.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static LCObject ParseObject(string json) {
            LCObjectData objectData = LCObjectData.Decode(JsonConvert.DeserializeObject<Dictionary<string, object>>(json));
            LCObject obj = Create(objectData.ClassName);
            obj.Merge(objectData);
            return obj;
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

        public void Merge(LCObjectData objectData) {
            Data.ClassName = objectData.ClassName ?? Data.ClassName;
            Data.ObjectId = objectData.ObjectId ?? Data.ObjectId;
            Data.CreatedAt = objectData.CreatedAt != null ? objectData.CreatedAt : Data.CreatedAt;
            Data.UpdatedAt = objectData.UpdatedAt != null ? objectData.UpdatedAt : Data.UpdatedAt;
            // 先将本地的预估数据直接替换
            ApplyCustomProperties();
            // 再将服务端的数据覆盖
            foreach (KeyValuePair<string, object> kv in objectData.CustomPropertyDict) {
                string key = kv.Key;
                object value = kv.Value;
                Data.CustomPropertyDict[key] = value;
            }

            // 最后重新生成预估数据，用于后续访问和操作
            RebuildEstimatedData();
            // 清空操作
            operationDict.Clear();
            isNew = false;
        }

        public void ApplyCustomProperties() {
            Data.CustomPropertyDict = estimatedData;
        }

        void RebuildEstimatedData() {
            estimatedData = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kv in Data.CustomPropertyDict) {
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
