using UnityEngine;

namespace LeanCloud.Storage.Internal.Storage {
    public class UnityStorage : IStorage {
        public string GetStoragePath() {
            return Application.persistentDataPath;
        }
    }
}
