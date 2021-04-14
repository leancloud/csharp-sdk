using UnityEngine;
using LeanCloud.Common;

namespace LeanCloud.Storage.Internal.Persistence {
    public class UnityPersistence : IPersistence {
        public string GetPersistencePath() {
            return Application.persistentDataPath;
        }
    }
}
