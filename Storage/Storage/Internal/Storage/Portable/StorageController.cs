using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace LeanCloud.Storage.Internal
{
    /// <summary>
    /// Implements `IStorageController` for PCL targets, based off of PCLStorage.
    /// </summary>
    public class StorageController : IStorageController
    {
        private class StorageDictionary : IStorageDictionary<string, object>
        {
            private readonly string filePath;

            private Dictionary<string, object> dictionary;            
            readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

            public StorageDictionary(string filePath)
            {
                this.filePath = filePath;
                dictionary = new Dictionary<string, object>();
            }

            internal Task SaveAsync()
            {
                string json;
                locker.EnterReadLock();
                json = Json.Encode(dictionary);
                locker.ExitReadLock();
                using (var sw = new StreamWriter(filePath)) {
                    return sw.WriteAsync(json);
                }
            }

            internal async Task LoadAsync()
            {
                using (var sr = new StreamReader(filePath)) {
                    var text = await sr.ReadToEndAsync();
                    Dictionary<string, object> result = null;
                    try {
                        result = Json.Parse(text) as Dictionary<string, object>;
                    } catch (Exception e) {
                        AVClient.PrintLog(e.Message);
                    }

                    locker.EnterWriteLock();
                    dictionary = result ?? new Dictionary<string, object>();
                    locker.ExitWriteLock();
                }
            }

            internal void Update(IDictionary<string, object> contents)
            {
                locker.EnterWriteLock();
                dictionary = contents.ToDictionary(p => p.Key, p => p.Value);
                locker.ExitWriteLock();
            }

            public Task AddAsync(string key, object value)
            {
                locker.EnterWriteLock();
                dictionary[key] = value;
                locker.ExitWriteLock();
                return SaveAsync();
            }

            public Task RemoveAsync(string key)
            {
                locker.EnterWriteLock();
                dictionary.Remove(key);
                locker.ExitWriteLock();
                return SaveAsync();
            }

            public bool ContainsKey(string key)
            {
                try {
                    locker.EnterReadLock();
                    return dictionary.ContainsKey(key);
                } finally {
                    locker.ExitReadLock();
                }
            }

            public IEnumerable<string> Keys
            {
                get {
                    try {
                        locker.EnterReadLock();
                        return dictionary.Keys;
                    } finally {
                        locker.ExitReadLock();
                    }
                }
            }

            public bool TryGetValue(string key, out object value)
            {
                try {
                    locker.EnterReadLock();
                    return dictionary.TryGetValue(key, out value);
                } finally {
                    locker.ExitReadLock();
                }
            }

            public IEnumerable<object> Values
            {
                get {
                    try {
                        locker.EnterReadLock();
                        return dictionary.Values;
                    } finally {
                        locker.ExitReadLock();
                    }
                }
            }

            public object this[string key]
            {
                get {
                    try {
                        locker.EnterReadLock();
                        return dictionary[key];
                    } finally {
                        locker.ExitReadLock();
                    }
                }
            }

            public int Count
            {
                get {
                    try {
                        locker.EnterReadLock();
                        return dictionary.Count;
                    } finally {
                        locker.ExitReadLock();
                    }
                }
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                try {
                    locker.EnterReadLock();
                    return dictionary.GetEnumerator();
                } finally {
                    locker.ExitReadLock();
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                try {
                    locker.EnterReadLock();
                    return dictionary.GetEnumerator();
                } finally {
                    locker.ExitReadLock();
                }
            }
        }

        private const string LeanCloudStorageFileName = "ApplicationSettings";
        private readonly TaskQueue taskQueue = new TaskQueue();
        private readonly Task<string> fileTask;
        private StorageDictionary storageDictionary;

        public StorageController(string fileNamePrefix)
        {
            fileTask = taskQueue.Enqueue(t => t.ContinueWith(_ =>
            {
                string path = $"{fileNamePrefix}_{LeanCloudStorageFileName}";
                File.CreateText(path);
                return path;
            }), CancellationToken.None);
        }

        public Task<IStorageDictionary<string, object>> LoadAsync()
        {
            return taskQueue.Enqueue(toAwait =>
            {
                return toAwait.ContinueWith(_ =>
                {
                    if (storageDictionary != null)
                    {
                        return Task.FromResult<IStorageDictionary<string, object>>(storageDictionary);
                    }

                    storageDictionary = new StorageDictionary(fileTask.Result);
                    return storageDictionary.LoadAsync().OnSuccess(__ => storageDictionary as IStorageDictionary<string, object>);
                }).Unwrap();
            }, CancellationToken.None);
        }

        public Task<IStorageDictionary<string, object>> SaveAsync(IDictionary<string, object> contents)
        {
            return taskQueue.Enqueue(toAwait =>
            {
                return toAwait.ContinueWith(_ =>
                {
                    if (storageDictionary == null)
                    {
                        storageDictionary = new StorageDictionary(fileTask.Result);
                    }

                    storageDictionary.Update(contents);
                    return storageDictionary.SaveAsync().OnSuccess(__ => storageDictionary as IStorageDictionary<string, object>);
                }).Unwrap();
            }, CancellationToken.None);
        }
    }
}