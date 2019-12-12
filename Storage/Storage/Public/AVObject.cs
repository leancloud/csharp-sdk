using LeanCloud.Storage.Internal;
using LeanCloud.Utilities;
using System;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;

namespace LeanCloud {
    public class AVObject : IEnumerable<KeyValuePair<string, object>> {
        static readonly HashSet<string> RESERVED_KEYS = new HashSet<string> {
            "objectId", "ACL", "createdAt", "updatedAt"
        };

        public string ClassName {
            get {
                return state.ClassName;
            }
        }

        [AVFieldName("objectId")]
        public string ObjectId {
            get {
                return state.ObjectId;
            }
            set {
                IsDirty = true;
                MutateState(mutableClone => {
                    mutableClone.ObjectId = value;
                });
            }
        }

        [AVFieldName("ACL")]
        public AVACL ACL {
            get {
                return GetProperty<AVACL>(null, "ACL");
            }
            set {
                IsDirty = true;
                MutateState(mutableClone => {
                    mutableClone.ACL = value;
                });
            }
        }

        [AVFieldName("createdAt")]
        public DateTime? CreatedAt {
            get {
                return state.CreatedAt;
            }
        }

        [AVFieldName("updatedAt")]
        public DateTime? UpdatedAt {
            get {
                return state.UpdatedAt;
            }
        }

        public ICollection<string> Keys {
            get {
                return estimatedData.Keys.Union(serverData.Keys).ToArray();
            }
        }

        private static readonly string AutoClassName = "_Automatic";

        internal readonly ConcurrentDictionary<string, IAVFieldOperation> operationDict = new ConcurrentDictionary<string, IAVFieldOperation>();
        private readonly ConcurrentDictionary<string, object> serverData = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, object> estimatedData = new ConcurrentDictionary<string, object>();

        private bool dirty;

        private IObjectState state;

        internal void MutateState(Action<MutableObjectState> func) {
            state = state.MutatedClone(func);
            RebuildEstimatedData();
        }

        public IObjectState State {
            get {
                return state;
            }
        }

        internal static AVObjectController ObjectController {
            get {
                return AVPlugins.Instance.ObjectController;
            }
        }

        internal static ObjectSubclassingController SubclassingController {
            get {
                return AVPlugins.Instance.SubclassingController;
            }
        }

        public static string GetSubClassName<TAVObject>() {
            return SubclassingController.GetClassName(typeof(TAVObject));
        }

        #region AVObject Creation

        protected AVObject()
            : this(AutoClassName) {
        }

        public AVObject(string className) {
            if (className == null) {
                throw new ArgumentException("You must specify a LeanCloud class name when creating a new AVObject.");
            }
            if (AutoClassName.Equals(className)) {
                className = SubclassingController.GetClassName(GetType());
            }
            if (!SubclassingController.IsTypeValid(className, GetType())) {
                throw new ArgumentException(
                  "You must create this type of AVObject using AVObject.Create() or the proper subclass.");
            }
            state = new MutableObjectState {
                ClassName = className
            };
            IsDirty = true;
        }

        public static AVObject Create(string className) {
            return SubclassingController.Instantiate(className);
        }

        public static AVObject CreateWithoutData(string className, string objectId) {
            var result = SubclassingController.Instantiate(className);
            result.ObjectId = objectId;
            result.IsDirty = false;
            if (result.IsDirty) {
                throw new InvalidOperationException("A AVObject subclass default constructor must not make changes to the object that cause it to be dirty.");
            }
            return result;
        }

        public static T Create<T>() where T : AVObject {
            return (T)SubclassingController.Instantiate(SubclassingController.GetClassName(typeof(T)));
        }

        public static T CreateWithoutData<T>(string objectId) where T : AVObject {
            return (T)CreateWithoutData(SubclassingController.GetClassName(typeof(T)), objectId);
        }

        public static T FromState<T>(IObjectState state, string defaultClassName) where T : AVObject {
            string className = state.ClassName ?? defaultClassName;

            T obj = (T)CreateWithoutData(className, state.ObjectId);
            obj.HandleFetchResult(state);

            return obj;
        }

        #endregion

        public static IDictionary<string, string> GetPropertyMappings(string className) {
            return SubclassingController.GetPropertyMappings(className);
        }

        private static string GetFieldForPropertyName(string className, string propertyName) {
            if (SubclassingController.GetPropertyMappings(className).TryGetValue(propertyName, out string fieldName)) {
                return fieldName;
            }
            return null;
        }

        protected virtual void SetProperty<T>(T value, string propertyName) {
            this[GetFieldForPropertyName(ClassName, propertyName)] = value;
        }

        protected AVRelation<T> GetRelationProperty<T>(string propertyName) where T : AVObject {
            return GetRelation<T>(GetFieldForPropertyName(ClassName, propertyName));
        }

        protected virtual T GetProperty<T>(string propertyName) {
            return GetProperty<T>(default, propertyName);
        }

        protected virtual T GetProperty<T>(T defaultValue, string propertyName) {
            if (TryGetValue(GetFieldForPropertyName(ClassName, propertyName), out T result)) {
                return result;
            }
            return defaultValue;
        }

        internal virtual void SetDefaultValues() {
        }

        public static void RegisterSubclass<T>() where T : AVObject, new() {
            SubclassingController.RegisterSubclass(typeof(T));
        }

        internal static void UnregisterSubclass<T>() where T : AVObject, new() {
            SubclassingController.UnregisterSubclass(typeof(T));
        }

        public void Revert() {
            if (operationDict.Any()) {
                operationDict.Clear();
                RebuildEstimatedData();
            }
        }

        internal virtual void HandleFetchResult(IObjectState serverState) {
            MergeFromServer(serverState);
        }

        internal virtual void HandleSave(IObjectState serverState) {
            state = state.MutatedClone((objectState) => objectState.Apply(operationDict));
            MergeFromServer(serverState);
        }

        public virtual void MergeFromServer(IObjectState serverState) {
            // Make a new serverData with fetched values.
            var newServerData = serverState.ToDictionary(t => t.Key, t => t.Value);

            // We cache the fetched object because subsequent Save operation might flush
            // the fetched objects into Pointers.
            //IDictionary<string, AVObject> fetchedObject = CollectFetchedObjects();

            //foreach (var pair in serverState) {
            //    var value = pair.Value;
            //    if (value is AVObject) {
            //        // Resolve fetched object.
            //        var avObject = value as AVObject;
            //        if (fetchedObject.TryGetValue(avObject.ObjectId, out AVObject obj)) {
            //            value = obj;
            //        }
            //    }
            //    newServerData[pair.Key] = value;
            //}

            IsDirty = false;
            serverState = serverState.MutatedClone(mutableClone => {
                mutableClone.ServerData = newServerData;
            });
            MutateState(mutableClone => {
                mutableClone.Apply(serverState);
            });
        }

        internal void MergeFromObject(AVObject other) {
            if (this == other) {
                return;
            }

            operationDict.Clear();
            foreach (KeyValuePair<string, IAVFieldOperation> entry in other.operationDict) {
                operationDict.AddOrUpdate(entry.Key, entry.Value, (key, value) => value);
            }
            state = other.State;

            RebuildEstimatedData();
        }

        public static IDictionary<string, object> ToJSONObjectForSaving(IDictionary<string, IAVFieldOperation> operations) {
            var result = new Dictionary<string, object>();
            foreach (var pair in operations) {
                // AVRPCSerialize the data
                var operation = pair.Value;

                result[pair.Key] = PointerOrLocalIdEncoder.Instance.Encode(operation);
            }
            return result;
        }

        internal IDictionary<string, object> EncodeForSaving(IDictionary<string, object> data) {
            var result = new Dictionary<string, object>();
            foreach (var key in data.Keys) {
                var value = data[key];
                result.Add(key, PointerOrLocalIdEncoder.Instance.Encode(value));
            }

            return result;
        }


        internal IDictionary<string, object> ServerDataToJSONObjectForSerialization() {
            return PointerOrLocalIdEncoder.Instance.Encode(state.ToDictionary(t => t.Key, t => t.Value))
                as IDictionary<string, object>;
        }

        #region Save Object()

        public virtual async Task SaveAsync(bool fetchWhenSave = false, AVQuery<AVObject> query = null, CancellationToken cancellationToken = default) {
            if (HasCircleReference(this, new HashSet<AVObject>())) {
                throw new AVException(AVException.ErrorCode.CircleReference, "Found a circle dependency when save");
            }
            Stack<Batch> batches = BatchObjects(new List<AVObject> { this }, false);
            await SaveBatches(batches, cancellationToken);
            IObjectState result = await ObjectController.SaveAsync(state, operationDict, fetchWhenSave, query, cancellationToken);
            HandleSave(result);
        }

        public static async Task SaveAllAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : AVObject {
            foreach (T obj in objects) {
                if (HasCircleReference(obj, new HashSet<AVObject>())) {
                    throw new AVException(AVException.ErrorCode.CircleReference, "Found a circle dependency when save");
                }
            }
            Stack<Batch> batches = BatchObjects(objects, true);
            await SaveBatches(batches, cancellationToken);
        }

        static async Task SaveBatches(Stack<Batch> batches, CancellationToken cancellationToken = default) {
            while (batches.Any()) {
                Batch batch = batches.Pop();
                IList<AVObject> dirtyObjects = batch.Objects.Where(o => o.IsDirty).ToList();
                var serverStates = await ObjectController.SaveAllAsync(dirtyObjects, cancellationToken);

                try {
                    foreach (var pair in dirtyObjects.Zip(serverStates, (item, state) => new { item, state })) {
                        pair.item.HandleSave(pair.state);
                    }
                } catch (Exception e) {
                    throw e;
                }
            }
        }

        internal virtual async Task<AVObject> FetchAsyncInternal(IDictionary<string, object> queryString, CancellationToken cancellationToken = default) {
            if (ObjectId == null) {
                throw new InvalidOperationException("Cannot refresh an object that hasn't been saved to the server.");
            }
            if (queryString == null) {
                queryString = new Dictionary<string, object>();
            }
            IObjectState objectState = await ObjectController.FetchAsync(state, queryString, cancellationToken);
            HandleFetchResult(objectState);
            return this;
        }

        #endregion

        #region Fetch Object(s)

        public static Task<IEnumerable<T>> FetchAllIfNeededAsync<T>(
            IEnumerable<T> objects, CancellationToken cancellationToken = default) where T : AVObject {
            return FetchAllInternalAsync(objects, false, cancellationToken);
        }

        public static Task<IEnumerable<T>> FetchAllAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default) where T : AVObject {
            return FetchAllInternalAsync(objects, true, cancellationToken);
        }

        private static Task<IEnumerable<T>> FetchAllInternalAsync<T>(
            IEnumerable<T> objects, bool force, CancellationToken cancellationToken) where T : AVObject {

            if (objects.Any(obj => obj.state.ObjectId == null)) {
                throw new InvalidOperationException("You cannot fetch objects that haven't already been saved.");
            }
            
            // Do one Find for each class.
            var findsByClass =
              (from obj in objects
               group obj.ObjectId by obj.ClassName into classGroup
               where classGroup.Any()
               select new {
                   ClassName = classGroup.Key,
                   FindTask = new AVQuery<AVObject>(classGroup.Key)
                   .WhereContainedIn("objectId", classGroup)
                   .FindAsync(cancellationToken)
               }).ToDictionary(pair => pair.ClassName, pair => pair.FindTask);

            // Wait for all the Finds to complete.
            return Task.WhenAll(findsByClass.Values.ToList()).OnSuccess(__ => {
                if (cancellationToken.IsCancellationRequested) {
                    return objects;
                }

                // Merge the data from the Finds into the input objects.
                var pairs = from obj in objects
                            from result in findsByClass[obj.ClassName].Result
                            where result.ObjectId == obj.ObjectId
                            select new { obj, result };
                foreach (var pair in pairs) {
                    pair.obj.MergeFromObject(pair.result);
                }

                return objects;
            });
        }

        #endregion

        #region Delete Object

        public virtual async Task DeleteAsync(AVQuery<AVObject> query = null, CancellationToken cancellationToken = default) {
            if (ObjectId == null) {
                return;
            }
            var command = new AVCommand {
                Path = $"classes/{state.ClassName}/{state.ObjectId}",
                Method = HttpMethod.Delete
            };
            if (query != null) {
                Dictionary<string, object> where = new Dictionary<string, object> {
                    { "where", query.BuildWhere() }
                };
                command.Path = $"{command.Path}?{AVClient.BuildQueryString(where)}";
            }
            await AVPlugins.Instance.CommandRunner.RunCommandAsync<IDictionary<string, object>>(command, cancellationToken);
            IsDirty = true;
        }

        public static async Task DeleteAllAsync<T>(IEnumerable<T> objects, CancellationToken cancellationToken = default)
            where T : AVObject {
            var uniqueObjects = new HashSet<AVObject>(objects.OfType<AVObject>().ToList(),
                new IdentityEqualityComparer<AVObject>());

            var states = uniqueObjects.Select(t => t.state);
            var requests = states
                .Where(item => item.ObjectId != null)
                .Select(item => new AVCommand {
                    Path = $"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}",
                    Method = HttpMethod.Delete
                })
                .ToList();
            await AVPlugins.Instance.CommandRunner.ExecuteBatchRequests(requests, cancellationToken);

            foreach (var obj in uniqueObjects) {
                obj.IsDirty = true;
            }
        }

        #endregion

        public virtual void Remove(string key) {
            CheckKeyValid(key);
            PerformOperation(key, AVDeleteOperation.Instance);
        }


        private IEnumerable<string> ApplyOperations(IDictionary<string, IAVFieldOperation> operations, IDictionary<string, object> map) {
            List<string> appliedKeys = new List<string>();
            foreach (var pair in operations) {
                map.TryGetValue(pair.Key, out object oldValue);
                var newValue = pair.Value.Apply(oldValue, pair.Key);
                if (newValue != AVDeleteOperation.DeleteToken) {
                    map[pair.Key] = newValue;
                } else {
                    map.Remove(pair.Key);
                }
                appliedKeys.Add(pair.Key);
            }
            return appliedKeys;
        }

        internal void RebuildEstimatedData() {
            estimatedData.Clear();
            ApplyOperations(operationDict, estimatedData);
        }

        internal void PerformOperation(string key, IAVFieldOperation operation) {
            estimatedData.TryGetValue(key, out object oldValue);
            object newValue = operation.Apply(oldValue, key);
            if (newValue != AVDeleteOperation.DeleteToken) {
                estimatedData[key] = newValue;
            } else {
                estimatedData.TryRemove(key, out _);
            }

            if (operationDict.TryGetValue(key, out IAVFieldOperation oldOperation)) {
                operation = operation.MergeWithPrevious(oldOperation);
            }
            operationDict[key] = operation;
        }

        internal virtual void OnSettingValue(ref string key, ref object value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
        }

        virtual public object this[string key] {
            get {
                CheckKeyValid(key);

                if (!estimatedData.TryGetValue(key, out object value)) {
                    value = state[key];
                }

                if (value is AVRelationBase) {
                    var relation = value as AVRelationBase;
                    relation.EnsureParentAndKey(this, key);
                }

                return value;
            }
            set {
                CheckKeyValid(key);
                Set(key, value);
            }
        }

        internal void Set(string key, object value) {
            OnSettingValue(ref key, ref value);
            PerformOperation(key, new AVSetOperation(value));
        }

        #region Atomic Increment

        public void Increment(string key) {
            Increment(key, 1);
        }

        public void Increment(string key, long amount) {
            CheckKeyValid(key);
            PerformOperation(key, new AVIncrementOperation(amount));
        }

        public void Increment(string key, double amount) {
            CheckKeyValid(key);
            PerformOperation(key, new AVIncrementOperation(amount));
        }

        #endregion

        public void AddToList(string key, object value) {
            CheckKeyValid(key);
            AddRangeToList(key, new[] { value });
        }

        public void AddRangeToList<T>(string key, IEnumerable<T> values) {
            CheckKeyValid(key);
            PerformOperation(key, new AVAddOperation(values.Cast<object>()));
        }

        public void AddUniqueToList(string key, object value) {
            CheckKeyValid(key);
            AddRangeUniqueToList(key, new object[] { value });
        }

        public void AddRangeUniqueToList<T>(string key, IEnumerable<T> values) {
            CheckKeyValid(key);
            PerformOperation(key, new AVAddUniqueOperation(values.Cast<object>()));
        }

        public void RemoveAllFromList<T>(string key, IEnumerable<T> values) {
            CheckKeyValid(key);
            PerformOperation(key, new AVRemoveOperation(values.Cast<object>()));
        }

        void CheckKeyValid(string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.StartsWith("_", StringComparison.CurrentCulture)) {
                throw new ArgumentException("key should not start with _");
            }
            if (RESERVED_KEYS.Contains(key)) {
                throw new ArgumentException($"key: {key} is reserved by LeanCloud");
            }
        }

        public bool ContainsKey(string key) {
            return estimatedData.ContainsKey(key) || state.ContainsKey(key);
        }

        public T Get<T>(string key) {
            return Conversion.To<T>(this[key]);
        }

        public AVRelation<T> GetRelation<T>(string key) where T : AVObject {
            // All the sanity checking is done when add or remove is called.
            TryGetValue(key, out AVRelation<T> relation);
            return relation ?? new AVRelation<T>(this, key);
        }

        public AVQuery<T> GetRelationRevserseQuery<T>(string parentClassName, string key) where T : AVObject {
            if (string.IsNullOrEmpty(parentClassName)) {
                throw new ArgumentNullException(nameof(parentClassName), "can not query a relation without parentClassName.");
            }
            if (string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException(nameof(key), "can not query a relation without key.");
            }
            return new AVQuery<T>(parentClassName).WhereEqualTo(key, this);
        }

        public virtual bool TryGetValue<T>(string key, out T result) {
            if (ContainsKey(key)) {
                try {
                    var temp = Conversion.To<T>(this[key]);
                    result = temp;
                    return true;
                } catch (InvalidCastException) {
                    result = default;
                    return false;
                }
            }
            result = default;
            return false;
        }

        public bool HasSameId(AVObject other) {
            return other != null &&
                    object.Equals(ClassName, other.ClassName) &&
                    object.Equals(ObjectId, other.ObjectId);
        }

        public bool IsDirty {
            get {
                return CheckIsDirty();
            }
            internal set {
                dirty = value;
            }
        }

        public bool IsKeyDirty(string key) {
            return operationDict.ContainsKey(key);
        }

        private bool CheckIsDirty() {
            return dirty || operationDict.Count > 0;
        }

        public void Add(string key, object value) {
            if (ContainsKey(key)) {
                throw new ArgumentException("Key already exists", key);
            }
            this[key] = value;
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
            return estimatedData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
        }

        public static AVQuery<T> GetQuery<T>(string className)
            where T : AVObject {
            return new AVQuery<T>(className);
        }

        #region refactor

        static bool HasCircleReference(object obj, HashSet<AVObject> parents) {
            if (parents.Contains(obj)) {
                return true;
            }
            IEnumerable deps = null;
            if (obj is IList) {
                deps = obj as IList;
            } else if (obj is IDictionary) {
                deps = (obj as IDictionary).Values;
            } else if (obj is AVObject) {
                deps = (obj as AVObject).estimatedData.Values;
            }
            HashSet<AVObject> depParent = new HashSet<AVObject>(parents);
            if (obj is AVObject) {
                depParent.Add(obj as AVObject);
            }
            if (deps != null) {
                foreach (object dep in deps) {
                    HashSet<AVObject> p = new HashSet<AVObject>(depParent);
                    if (HasCircleReference(dep, p)) {
                        return true;
                    }
                }
            }
            return false;
        }

        static Stack<Batch> BatchObjects(IEnumerable<AVObject> avObjects, bool containsSelf) {
            Stack<Batch> batches = new Stack<Batch>();
            if (containsSelf) {
                batches.Push(new Batch(avObjects));
            }

            IEnumerable<object> deps = avObjects.Select(avObj => avObj.estimatedData.Values);
            do { 
                HashSet<object> childSets = new HashSet<object>();
                foreach (object dep in deps) {
                    IEnumerable children = null;
                    if (dep is IList) {
                        children = dep as IList;
                    } else if (dep is IDictionary) {
                        children = (dep as IDictionary).Values;
                    } else if (dep is AVObject && (dep as AVObject).ObjectId == null) {
                        // 如果依赖是 AVObject 类型并且还没有保存过，则应该遍历其依赖
                        // 这里应该是从 Operation 中查找新增的对象
                        children = (dep as AVObject).estimatedData.Values;
                    }
                    if (children != null) {
                        foreach (object child in children) {
                            childSets.Add(child);
                        }
                    }
                }
                IEnumerable<AVObject> depAVObjs = deps.OfType<AVObject>().Where(o => o.ObjectId == null);
                if (depAVObjs.Any()) {
                    batches.Push(new Batch(depAVObjs));
                }
                deps = childSets;
            } while (deps != null && deps.Any());

            return batches;
        }

        #endregion

        /// <summary>
        /// 保存 AVObject 时用到的辅助批次工具类
        /// </summary>
        internal class Batch {
            internal HashSet<AVObject> Objects {
                get; set;
            }

            public Batch() {
                Objects = new HashSet<AVObject>();
            }

            public Batch(IEnumerable<AVObject> objects) : this() {
                foreach (AVObject obj in objects) {
                    Objects.Add(obj);
                }
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("----------------------------");
                foreach (AVObject obj in Objects) {
                    sb.AppendLine(obj.ClassName);
                }
                sb.AppendLine("----------------------------");
                return sb.ToString();
            }
        }
    }
}
