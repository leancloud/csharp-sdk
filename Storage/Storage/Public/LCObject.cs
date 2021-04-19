using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LC.Newtonsoft.Json;
using LeanCloud.Common;
using LeanCloud.Storage.Internal.Object;
using LeanCloud.Storage.Internal.Operation;
using LeanCloud.Storage.Internal.Codec;

namespace LeanCloud.Storage {
    /// <summary>
    /// The LCObject is a local representation of data that can be saved and
    /// retrieved from the LeanCloud.
    /// </summary>
    public partial class LCObject {
        internal LCObjectData data;

        internal Dictionary<string, object> estimatedData;

        internal Dictionary<string, ILCOperation> operationDict;

        static readonly Dictionary<Type, LCSubclassInfo> subclassTypeDict = new Dictionary<Type, LCSubclassInfo>();
        static readonly Dictionary<string, LCSubclassInfo> subclassNameDict = new Dictionary<string, LCSubclassInfo>();

        /// <summary>
        /// Gets the class name for the LCObject.
        /// </summary>
        public string ClassName {
            get {
                return data.ClassName;
            }
        }

        /// <summary>
        /// Gets the object id. An object id is assigned as son as an object is
        /// saved to the server. The combination of a <see cref="ClassName"/> and
        /// an <see cref="ObjectId"/> uniquely identifies an object in your application.
        /// </summary>
        public string ObjectId {
            get {
                return data.ObjectId;
            }
        }

        /// <summary>
        /// Gets the first time of this object was saved the server.
        /// </summary>
        public DateTime CreatedAt {
            get {
                return data.CreatedAt;
            }
        }

        /// <summary>
        /// Gets the last time of this object was updated the server.
        /// </summary>
        public DateTime UpdatedAt {
            get {
                return data.UpdatedAt;
            }
        }

        /// <summary>
        /// Gets or sets the LCACL governing this object.
        /// </summary>
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

        /// <summary>
        /// Constructs a new LCObject with no data in it. A LCObject constructed
        /// in this way will not have an ObjectedId and will not persist to database
        /// until <see cref="Save(bool, LCQuery{LCObject}))"/> is called.
        /// </summary>
        /// <param name="className">The className for the LCObject.</param>
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

        /// <summary>
        /// Creates a reference to an existing LCObject for use in creating
        /// associations between LCObjects.
        /// </summary>
        /// <param name="className">The className for the LCObject.</param>
        /// <param name="objectId">The object id for the LCObject.</param>
        /// <returns></returns>
        public static LCObject CreateWithoutData(string className, string objectId) {
            if (string.IsNullOrEmpty(objectId)) {
                throw new ArgumentNullException(nameof(objectId));
            }
            LCObject obj = Create(className);
            obj.data.ObjectId = objectId;
            obj.isNew = false;
            return obj;
        }

        /// <summary>
        /// Creates a reference to an existing LCObject for use in creating
        /// associations between LCObjects.
        /// </summary>
        /// <param name="className">The className for the LCObject.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets or sets a value on the object. It is forbidden to name keys
        /// with '_'.
        /// </summary>
        /// <param name="key">The value for key.</param>
        /// <returns></returns>
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


        /// <summary>
        /// Creates a <see cref="LCRelation{T}"/> value for a key.
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
        /// Removes a <see cref="LCRelation{T}"/> value for a key.
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

                List<Dictionary<string, object>> results = await LCCore.HttpClient.Post<List<Dictionary<string, object>>>("batch", data: data);
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

        /// <summary>
        /// Saves this object to the server.
        /// </summary>
        /// <param name="fetchWhenSave">Whether or not fetch data when saved.</param>
        /// <param name="query">The condition for saving.</param>
        /// <returns></returns>
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
                await LCCore.HttpClient.Post<Dictionary<string, object>>(path, data: LCEncoder.Encode(operationDict) as Dictionary<string, object>, queryParams: queryParams) :
                await LCCore.HttpClient.Put<Dictionary<string, object>>(path, data: LCEncoder.Encode(operationDict) as Dictionary<string, object>, queryParams: queryParams);
            LCObjectData data = LCObjectData.Decode(response);
            Merge(data);
            return this;
        }

        /// <summary>
        /// Saves each object in the provided list.
        /// </summary>
        /// <param name="objects">The objects to save.</param>
        /// <returns></returns>
        public static async Task<List<LCObject>> SaveAll(IEnumerable<LCObject> objects) {
            if (objects == null) {
                throw new ArgumentNullException(nameof(objects));
            }
            foreach (LCObject obj in objects) {
                if (LCBatch.HasCircleReference(obj, new HashSet<LCObject>())) {
                    throw new ArgumentException("Found a circle dependency when save.");
                }
            }
            Stack<LCBatch> batches = LCBatch.BatchObjects(objects, true);
            await SaveBatches(batches);
            return objects.ToList();
        }

        /// <summary>
        /// Deletes this object on the server.
        /// </summary>
        /// <returns></returns>
        public async Task Delete() {
            if (ObjectId == null) {
                return;
            }
            string path = $"classes/{ClassName}/{ObjectId}";
            await LCCore.HttpClient.Delete(path);
        }

        /// <summary>
        /// Deletes each object in the provided list.
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static async Task DeleteAll(IEnumerable<LCObject> objects) {
            if (objects == null || objects.Count() == 0) {
                throw new ArgumentNullException(nameof(objects));
            }
            HashSet<LCObject> objectSet = new HashSet<LCObject>(objects.Where(item => item.ObjectId != null));
            List<Dictionary<string, object>> requestList = objectSet.Select(item => {
                string path = $"/{LCCore.APIVersion}/classes/{item.ClassName}/{item.ObjectId}";
                return new Dictionary<string, object> {
                    { "path", path },
                    { "method", "DELETE" }
                };
            }).ToList();
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "requests", LCEncoder.Encode(requestList) }
            };
            await LCCore.HttpClient.Post<List<object>>("batch", data: data);
        }

        /// <summary>
        /// Fetches this object from server.
        /// </summary>
        /// <param name="keys">The keys for fetching.</param>
        /// <param name="includes">The include keys for fetching.</param>
        /// <returns></returns>
        public async Task<LCObject> Fetch(IEnumerable<string> keys = null, IEnumerable<string> includes = null) {
            Dictionary<string, object> queryParams = new Dictionary<string, object>();
            if (keys != null) {
                queryParams["keys"] = string.Join(",", keys);
            }
            if (includes != null) {
                queryParams["include"] = string.Join(",", includes);
            }
            string path = $"classes/{ClassName}/{ObjectId}";
            Dictionary<string, object> response = await LCCore.HttpClient.Get<Dictionary<string, object>>(path, queryParams: queryParams);
            LCObjectData objectData = LCObjectData.Decode(response);
            Merge(objectData);
            return this;
        }

        /// <summary>
        /// Fetches all of the objects in the provided list.
        /// </summary>
        /// <param name="objects">The objects for fetching.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<LCObject>> FetchAll(IEnumerable<LCObject> objects) {
            if (objects == null || objects.Count() == 0) {
                throw new ArgumentNullException(nameof(objects));
            }

            IEnumerable<LCObject> uniqueObjects = objects.Where(item => item.ObjectId != null);
            List<Dictionary<string, object>> requestList = uniqueObjects.Select(item => {
                string path = $"/{LCCore.APIVersion}/classes/{item.ClassName}/{item.ObjectId}";
                return new Dictionary<string, object> {
                    { "path", path },
                    { "method", "GET" }
                };
            }).ToList();

            Dictionary<string, object> data = new Dictionary<string, object> {
                { "requests", LCEncoder.Encode(requestList) }
            };
            List<Dictionary<string, object>> results = await LCCore.HttpClient.Post<List<Dictionary<string, object>>>("batch",
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

        /// <summary>
        /// Registers a custom subclass type with LeanCloud SDK, enabling strong-typing
        /// of those LCObjects whenever they appear.
        /// </summary>
        /// <typeparam name="T">The LCObject subclass type to register.</typeparam>
        /// <param name="className">The className on server.</param>
        /// <param name="constructor">The constructor for creating an object.</param>
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
            Dictionary<string, object> originalData = LCObjectData.Encode(data);
            Dictionary<string, object> currentData = estimatedData.Union(originalData.Where(kv => !estimatedData.ContainsKey(kv.Key)))
                .ToDictionary(k => k.Key, v => LCEncoder.Encode(v.Value));
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
